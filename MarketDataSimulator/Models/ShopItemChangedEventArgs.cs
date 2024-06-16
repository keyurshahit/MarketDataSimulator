namespace MarketDataSimulator.Models
{
    public class ShopItemChangedEventArgs
    {
        public ShopItemChangedEventArgs(List<ShopItem> newValues, List<long> deletedValues)
        {
            NewValues = newValues;
            DeletedIds = deletedValues;
        }

        public List<ShopItem> NewValues { get; }
        public List<long> DeletedIds { get; }
    }
}
