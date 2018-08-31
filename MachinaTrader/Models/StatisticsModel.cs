using System;
using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Models
{
    public class Statistics
    {
        public decimal? CurrentBalance { get; set; } = 0m;
        public decimal? CurrentBalancePerformance { get; set; } = 0;
        public decimal? InvestedCoins { get; set; } = 0m;
        public decimal? InvestedCoinsPerformance { get; set; } = 0;
        public decimal? ProfitLoss { get; set; } = 0m;
        public decimal? ProfitLossPercentage { get; set; } = 0;
        public int? PositiveTrades { get; set; }
        public int? NegativeTrades { get; set; }

        public List<CoinPerformance> CoinPerformances { get; set; }
    }

    public class CoinPerformance
    {
        public string Coin { get; set; }
        public decimal? InvestedCoins { get; set; } = 0m;
        public decimal? Performance { get; set; } = 0m;
        public decimal? PerformancePercentage { get; set; } = 0;
        public int? PositiveTrades { get; set; }
        public int? NegativeTrades { get; set; }
    }

    public class WalletStatistic
    {
        public List<DateTime> Dates { get; set; }
        public List<decimal> Amounts { get; set; }
        public List<decimal> Balances { get; set; }
    }
}
