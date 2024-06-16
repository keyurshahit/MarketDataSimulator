namespace MarketDataSimulator.Models
{
    public class ShopItem
    {
        public long Id { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public DateTime Updated { get; protected set; }
        public int BestBidPrice { get; protected set; }
        public int BestBidQuantity { get; protected set; }
        public int BestOfferPrice { get; protected set; }
        public int BestOfferQuantity { get; protected set; }

        public ShopItem(long id)
        {
            Id = id;
        }

        public ShopItem(long id, string name, string description, DateTime updated, int bestBidPrice, int bestBidQuantity, int bestOfferPrice, int bestOfferQuantity)
        {
            Id = id;
            Name = name;
            Description = description;
            Updated = updated;
            BestBidPrice = bestBidPrice;
            BestBidQuantity = bestBidQuantity;
            BestOfferPrice = bestOfferPrice;
            BestOfferQuantity = bestOfferQuantity;
        }
    }
}
