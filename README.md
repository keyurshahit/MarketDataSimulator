# MarketDataSimulator
- C#/asp.net core websocket server app for bi-directional data streaming with pub/sub
- Upon initial client connection the server sends back a full list of available product_ids that client will use to subscribe/unsubscribe to with a follow up request
- Server accepts the following subscribe/unsubscribe JSON requests with a list of product_id's:
	- {"type":"subscribe","product_ids":[<list of id's>]}
	- {"type":"unsubscribe","product_ids":[<list of id's>]}
- Config values MaxRows and RefreshRateMs controls how many maximum rows of data could be sent to the client and how frequently
- Current impl communicates data in Text format (WebSocketMessageType.Text). Depending on the data type this could be optimized by compressing and sending as Binary
