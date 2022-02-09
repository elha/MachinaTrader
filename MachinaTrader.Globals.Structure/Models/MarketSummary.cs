namespace MachinaTrader.Globals.Structure.Models
{
    public class MarketSummary
    {
        public CurrencyPair CurrencyPair { get; set; }
        public string MarketName { get; set; }
        public decimal Volume { get; set; }
        public decimal Last { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public string GlobalMarketName {
            get
            {
                return CurrencyPair.BaseCurrency + "-" + CurrencyPair.QuoteCurrency;
            }
        }
    }
}
