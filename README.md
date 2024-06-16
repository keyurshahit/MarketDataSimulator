# MarketDataSimulator
- C#/asp.net core websocket server app for bi-directional data streaming with pub/sub
- Upon initial client connection the server sends back a full list of available product_ids that client will use to subscribe/un-subscribe to with a follow up request
- Server accepts the following subscribe/un-subscribe JSON requests with a list of product_id's:
 	```json
  	{"type":"subscribe","product_ids":[<list of id's>]}
  	```
  	```json
	{"type":"unsubscribe","product_ids":[<list of id's>]}
   	```
> [!NOTE]
> - Config values ```MaxRows``` and ```RefreshRateMs``` controls how many maximum rows of data could be sent to the client and how frequently
> - Current impl communicates data in Text format (```WebSocketMessageType.Text```). Depending on the data type this could be optimized by compressing and sending as Binary
> - Subscribe/un-subscribe requests could be made more granular and efficient by allowing the client to provide a list of specific columns along with product_ids to subscribe/un-subscribe to for change/updates
> - In a more formal application the client will go through proper authentication/authorization and will have the ability to create custom subscription profiles with selected tickers and subscribe by those profiles for ticking data instead of selecting/unselecting individual tickers each time for subscription
