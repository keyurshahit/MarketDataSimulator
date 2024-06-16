using MarketDataSimulator.Models;

namespace MarketDataSimulator.Simulator
{
    public interface IMarketSimulator
    {
        int MaxRows { get; set; }

        double RefreshRateMilliseconds { get; set; }

        event EventHandler<ShopItemChangedEventArgs>? ShopItemChanged;

        IEnumerable<ShopItem> GetShopItems(int rows);
    }
}
