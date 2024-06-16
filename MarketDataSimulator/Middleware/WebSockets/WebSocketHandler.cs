using MarketDataSimulator.Models;
using MarketDataSimulator.Simulator;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MarketDataSimulator.Middleware.WebSockets
{
    /// <summary>
    /// Websocket handler middleware
    /// </summary>
    public class WebSocketHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketHandler> _logger;
        private readonly IMarketSimulator _marketSimulator;
        private readonly ConcurrentDictionary<WebSocket, HashSet<long>> _subscriptions = new();
        private ConcurrentDictionary<long, ShopItem> _shopItems;

        /// <summary>
        /// Constructor init
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        /// <param name="marketSimulator"></param>
        public WebSocketHandler(RequestDelegate next, ILogger<WebSocketHandler> logger, IMarketSimulator marketSimulator)
        {
            _next = next;
            _logger = logger;
            _marketSimulator = marketSimulator;
            _shopItems = new ConcurrentDictionary<long, ShopItem>(_marketSimulator.GetShopItems(_marketSimulator.MaxRows).ToDictionary(x => x.Id, y => y));
            _marketSimulator.ShopItemChanged += MarketSimulator_ShopItemChanged;
        }

        /// <summary>
        /// Middleware Invoke
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // check for websocket connection
                //
                if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
                {
                    // accept incoming websocket client connection and add to internal map
                    //
                    var socket = await context.WebSockets.AcceptWebSocketAsync();
                    _subscriptions.TryAdd(socket, new HashSet<long>());

                    _logger.LogInformation($"Total incoming websocket connections: {_subscriptions.Count}");

                    // Send initial list of all ShopItem names to the new client
                    //
                    await SendInitialProductList(socket);

                    // wait for the next client message
                    //
                    await ReceiveAsync(socket);
                }
                else
                {
                    // call next handler in the pipeline
                    //
                    await _next(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing middleware {MethodBase.GetCurrentMethod()?.Name} => {ex}");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Server error");
            }
        }

        /// <summary>
        /// Method to send the full products list back to the websocket client for them to use to subscribe to the selected products
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private async Task SendInitialProductList(WebSocket socket)
        {
            var productList = _shopItems.Values.Select(item => new { item.Id, Name = $"{item.Name}-{item.Description}" }).ToList();
            var initialData = JsonSerializer.Serialize(productList);
            var buffer = Encoding.UTF8.GetBytes(initialData);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Method to process incoming websocket client messages
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private async Task ReceiveAsync(WebSocket socket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;
            try
            {
                // run the receive loop for each websocket while its open to read any incoming messages
                //
                while (socket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using var reader = new StreamReader(ms, Encoding.UTF8);
                        var message = await reader.ReadToEndAsync();
                        HandleMessage(socket, message);
                    }

                    // breakout upon client close status
                    //
                    if (result.CloseStatus.HasValue)
                        break;
                }

                // close client connection
                //
                await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing {MethodBase.GetCurrentMethod()?.Name} => {ex}");
            }
            finally
            {
                // clean up
                //
                _subscriptions.TryRemove(socket, out _);
                _logger.LogInformation($"Websocket connection closed. Subscribed client counts: {_subscriptions.Count}");
            }
        }

        /// <summary>
        /// Method to handle incoming websocket client messages
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        private async void HandleMessage(WebSocket socket, string message)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(message);
                if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();
                    if (type == "subscribe" && jsonDoc.RootElement.TryGetProperty("product_ids", out var subscribeProductIds))
                    {
                        foreach (var productId in subscribeProductIds.EnumerateArray())
                        {
                            if (productId.TryGetInt64(out var id))
                                _subscriptions[socket].Add(id);
                        }

                        // send initial values for subscribed ids
                        //                        
                        await SendInitialSubscribedItems(socket);

                        _logger.LogInformation($"Initial subscription data sent for product_ids: {_subscriptions[socket].Count}");
                    }
                    else if (type == "unsubscribe")
                    {
                        if (jsonDoc.RootElement.TryGetProperty("product_ids", out var unsubscribeProductIds))
                        {
                            foreach (var productId in unsubscribeProductIds.EnumerateArray())
                            {
                                if (productId.TryGetInt64(out var id))
                                    _subscriptions[socket].Remove(id);
                            }
                        }
                        else
                        {
                            _subscriptions[socket].Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message {message}: {ex}");
            }
        }

        /// <summary>
        /// Method to send initial subscribed items back to the websocket client
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private async Task SendInitialSubscribedItems(WebSocket socket)
        {
            List<ShopItem> subscribedItems = new List<ShopItem>();
            foreach (var id in _subscriptions[socket])
            {
                ShopItem item = null;
                if (_shopItems.TryGetValue(id, out item))
                    subscribedItems.Add(item);
            }

            var initialData = JsonSerializer.Serialize(subscribedItems);
            var buffer = Encoding.UTF8.GetBytes(initialData);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// ShopItemChanged event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarketSimulator_ShopItemChanged(object? sender, ShopItemChangedEventArgs e)
        {
            try
            {
                // Consolidate the list of affected shop items before sending to clients
                //
                if (_subscriptions.Count > 0)
                {
                    var consolidatedList = ConsolidateShopItems(e.NewValues, e.DeletedIds.ToHashSet());

                    // Send updates to all connected clients in parallel
                    //
                    Parallel.ForEach(_subscriptions.Keys, async (socket) =>
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            var subscribedItems = _subscriptions[socket];
                            var itemsToSend = consolidatedList.Where(item => subscribedItems.Contains(item.Id)).ToList();
                            if (itemsToSend.Any())
                            {
                                var dataToSend = JsonSerializer.Serialize(itemsToSend);
                                var sendBuffer = Encoding.UTF8.GetBytes(dataToSend);
                                await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing {MethodBase.GetCurrentMethod()?.Name} => {ex}");
            }
        }

        /// <summary>
        /// Method to consolidate items to send back to client into a single list. 
        /// i.e. remove redundant updates and send deletes as part of the same list with just the Id populated
        /// </summary>
        /// <param name="updatedItems"></param>
        /// <param name="deletedItemIds"></param>
        /// <returns></returns>
        private List<ShopItem> ConsolidateShopItems(IEnumerable<ShopItem> updatedItems, IEnumerable<long> deletedItemIds)
        {
            var consolidatedList = new List<ShopItem>();

            // Add updated items with only changed fields
            //
            foreach (var updatedItem in updatedItems)
            {
                if (_shopItems.TryGetValue(updatedItem.Id, out ShopItem originalItem)
                    && (updatedItem.BestBidPrice != originalItem.BestBidPrice
                        || updatedItem.BestBidQuantity != originalItem.BestBidQuantity
                        || updatedItem.BestOfferPrice != originalItem.BestOfferPrice
                        || updatedItem.BestOfferQuantity != originalItem.BestOfferQuantity))
                {
                    originalItem = updatedItem;
                    consolidatedList.Add(originalItem);
                }
            }

            // Add deleted items
            //
            foreach (var deletedId in deletedItemIds)
            {
                if (_shopItems.TryRemove(deletedId, out _))
                    consolidatedList.Add(new ShopItem(deletedId));
            }

            foreach (var item in updatedItems)
            {
                _shopItems[item.Id] = item;
            }

            return consolidatedList;
        }
    }
}