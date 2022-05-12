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

        public string TradeTimer { get; set; } = "0/30 * * * * ?";

        // Trading mode default is PaperTradeManager
        public bool PaperTrade { get; set; } = true;

        // Trader settings
        public int MaxOpenTimeBuy { get; set; } = 300;
        public decimal AmountToInvestPerTrade { get; set; } = 120m;


        // Default strategy to use with trade managers.
        public string DefaultUpStrategy { get; set; } = "BuyTheDip3:037";
        public string DefaultSideStrategy { get; set; } = "BuyTheDip3:187";


        // These are the markets we don't want to trade on
        public string QuoteCurrency { get; set; } = "USD";


        // These are the markets we want to trade 
        public string TradeAssets { get; set; } = "ETH,BTC";
        public string[] TradeAssetsList()
        {
            return TradeAssets.Split(new char[] {','}, System.StringSplitOptions.RemoveEmptyEntries);
        }

    }

    public class DisplayOptions
    {
        // Display currency
        public string DisplayFiatCurrency { get; set; } = "USD";
    }
}
