using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Models
{
    public class BalanceEntry
    {
        // Balance
        public string DisplayCurrency { get; set; }
        public string Market { get; set; }
        public decimal? TotalCoins { get; set; } = 0;
        public decimal? BalanceInBtc { get; set; } = 0;
        public decimal? BalanceInUsd { get; set; } = 0;
        public decimal? BalanceInDisplayCurrency{ get; set; } = 0;
    }
}
