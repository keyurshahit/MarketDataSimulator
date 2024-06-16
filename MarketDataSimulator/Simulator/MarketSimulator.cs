using MarketDataSimulator.Models;
using System.Text.Json;

namespace MarketDataSimulator.Simulator
{
    public class MarketSimulator : IMarketSimulator
    {        
        private readonly Random _rnd = new();
        private readonly List<ShopItem?> AllShopItems = new();
        
        public int MaxRows { get; set; }
        public double RefreshRateMilliseconds { get; set; }

        public MarketSimulator(int maxRows, int refreshRateMs)
        {            
            MaxRows = maxRows;
            RefreshRateMilliseconds = refreshRateMs;
            var json = File.ReadAllText("Data\\Items.json");
            var items = JsonSerializer.Deserialize<List<Item>>(json)!;
            AllShopItems.AddRange(items.Select(item => NewItem(item.Id, item.Name, item.Description)));
            Console.WriteLine($"Found {AllShopItems.Count:N0} shop items.");
            Console.WriteLine($"MaxRows: {MaxRows}");
            Console.WriteLine($"Refresh Rate (Ms): {RefreshRateMilliseconds}");
            _ = UpdateShop();
        }

        public IEnumerable<ShopItem> GetShopItems(int rows)
        {
            // By using indices, we can avoid version checks when iterating over the list.
            for (int i = 0, j = 0; j < rows && i < AllShopItems.Count; i++)
            {
                var item = AllShopItems[i];
                if (item != null)
                {
                    j++;
                    yield return item;
                }
            }
        }

        private ShopItem NewItem(long id, string name, string description)
        {
            return new ShopItem(id, name, description,
                        DateTime.Now, _rnd.Next(500), _rnd.Next(5000), _rnd.Next(500), _rnd.Next(5000));
        }

        private async Task UpdateShop()
        {
            try
            {                
                ShopItem[] originalItems = AllShopItems.OfType<ShopItem>().Take(MaxRows).ToArray();

                while (true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(RefreshRateMilliseconds));

                    List<ShopItem> updatedList = new();
                    List<long> deletedList = new();

                    int count = Math.Min(MaxRows, AllShopItems.Count);
                    for (int i = 0; i < count; i++)
                    {
                        int rnd = _rnd.Next(100);
                        var shopItem = AllShopItems[i];
                        if (rnd < 5 && shopItem is not null)
                        {
                            // Delete item
                            AllShopItems[i] = null;
                            deletedList.Add(i);
                        }
                        else if (rnd < 10)
                        {
                            // Inserted or updated item
                            shopItem ??= originalItems[i]; // if previously deleted, use original item
                            var newItem = NewItem(shopItem.Id, shopItem.Name, shopItem.Description);
                            AllShopItems[i] = newItem;
                            updatedList.Add(newItem);
                        }
                    }

                    //Console.WriteLine($"Update event => updatedList.Count: {updatedList.Count} deletedList.Count: {deletedList.Count}");
                    ShopItemChanged?.Invoke(this, new ShopItemChangedEventArgs(updatedList, deletedList));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public event EventHandler<ShopItemChangedEventArgs>? ShopItemChanged;
    }
}
