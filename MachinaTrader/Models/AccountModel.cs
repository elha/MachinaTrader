using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Models
{
    public class BalanceEntry
    {
        // Balance
        public string QuoteCurrrency { get; set; }
        public string Market { get; set; }
        public decimal? TotalCoins { get; set; } = 0;
        public decimal? BalanceValueQuoteCurrency{ get; set; } = 0;
        public decimal? BalanceValueUsd { get; set; } = 0;
    }
}
