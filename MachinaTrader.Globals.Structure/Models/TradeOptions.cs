using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Globals.Structure.Models
{
    public class Roi
    {
        public int Duration { get; set; }
        public decimal Profit { get; set; }
    }

    public class TradeOptions
    {

        public bool BuyEnabled { get; set; } = true;
        public bool SellEnabled { get; set; } = true;

        public string BuyTimer { get; set; } = "0 0/1 * * * ?";
        public string SellTimer { get; set; } = "0 0/1 * * * ?";

        // Trading mode default is PaperTradeManager
        public bool PaperTrade { get; set; } = true;

        // Trader settings
        public int MaxNumberOfConcurrentTrades { get; set; } = 1;
        public int MaxOpenTimeBuy { get; set; } = 300;
        public decimal AmountToInvestPerTrade { get; set; } = 0.005m;
        public decimal AmountToReinvestPercentage { get; set; } = 0.25m; //25% of wallet


        // Default strategy to use with trade managers.
        public string DefaultStrategy { get; set; } = "BuyTheDip";


        // These are the markets we don't want to trade on
        public string QuoteCurrency { get; set; } = "BTC";


        // These are the markets we want to trade 
        public string TradeAssets { get; set; } = "ETH,BTC";
        public string[] TradeAssetsList()
        {
            return TradeAssets.Split(',');
        }

    }

    public class DisplayOptions
    {
        // Display currency
        public string DisplayFiatCurrency { get; set; } = "USD";
    }
}
