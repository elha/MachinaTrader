using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;

namespace MachinaTrader.Globals.Structure.Models
{
    public class Trade
    {
        // Used as primary key for the different data storage mechanisms.
        public int Id { get; set; }

        public string TradeId { get; set; }
        public string TraderId { get; set; }
        public string Market { get; set; }

        public decimal OpenRate { get; set; }
        public decimal? CloseRate { get; set; }

        /// <summary>
        /// invested sum in USD
        /// </summary>
        public decimal StakeAmount { get; set; }

        /// <summary>
        /// sum in USD
        /// </summary>
        public decimal? CloseProfit { get; set; }
        public decimal? CloseProfitPercentage { get; set; }


        public decimal Quantity { get; set; }

        /// <summary>
        /// is position still open
        /// </summary>
        public bool IsOpen { get; set; }
        public bool IsBuying { get; set; }
        public bool IsSelling { get; set; }

        public string OpenOrderId { get; set; }
        public string BuyOrderId { get; set; }
        public string SellOrderId { get; set; }

        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }

        public string StrategyUsed { get; set; }
        public decimal? StopLossRate { get; set; }

        public BuyType BuyType { get; set; }
        public PositionType PositionType { get; set; }
        public SellType SellType { get; set; }

        public Trade()
        {
            TradeId = Guid.NewGuid().ToString().Replace("-", string.Empty);
            IsOpen = true;
            OpenDate = DateTime.UtcNow;
        }

        // Used for MyntUI output
        private Ticker _TickerLast;
        public Ticker TickerLast {
            get
            {
                return _TickerLast;
            }

            set
            {
                _TickerLast = value;

                if (OpenRate == 0) return; // no performance without rate

                // append Performance to History
                List<decimal> t;
                if (PerformanceHistory != null && PerformanceHistory.Length > 0)
                    t = new List<decimal>(PerformanceHistory);
                else
                    t = new List<decimal>();

                if (t.Count > 500) return; // prevent overflows

                if (PositionType == PositionType.Long)
                    t.Add((value.Mid() - OpenRate) / OpenRate * 100m);
                else
                    t.Add((OpenRate - value.Mid()) / OpenRate * 100m);

                PerformanceHistory = t.ToArray();
            }
        }

        public Ticker TickerMin { get; set; }
        public Ticker TickerMax { get; set; }
        public decimal[] PerformanceHistory { get; set; }

        //Add Options for this trade
        public decimal SellOnPercentage { get; set; } = (decimal)0.0;
        public bool HoldPosition { get; set; } = false;
        public bool SellNow { get; set; } = false;
        public string GlobalSymbol { get; set; }
        public string Exchange { get; set; }
        public bool IsPaperTrading { get; set; }
        public DateTime? DcaDate { get; set; }

        public decimal TradePerformance
        {
            get
            {
                if (OpenRate == 0) return 0;
                var nClose = (CloseRate.HasValue) ? CloseRate.Value : TickerLast.Mid();

                if (PositionType == PositionType.Long)
                    return (nClose - OpenRate) / OpenRate * 100m;
                else
                    return (OpenRate - nClose) / OpenRate * 100m;

            }
            set
            { // ignore
            }
        }

        public string TradePerformanceRange
        {
            get
            {
              
                return $"{TradePerformanceMin:N2}/{TradePerformanceMax:N2}";
                
            }
            set
            { // ignore
            }
        }
        // always positive value, shows current loss in USD 
        public decimal RiskValue
        {
            get
            {
                var perf = TradePerformance;
                if (perf >= 0) return 0m;
                return -perf * StakeAmount / 100.0m;
            }
        }

        public decimal? TradePerformanceMax
        {
            get
            {
                if (OpenRate == 0 || TickerMax == null || TickerMin == null) return null;
                var maxPerf = 0m;
                if (PositionType == PositionType.Long)
                    maxPerf = (TickerMax.Mid() - OpenRate) / OpenRate * 100m;
                else
                    maxPerf = (OpenRate - TickerMin.Mid()) / OpenRate * 100m;

                return maxPerf;
            }
        }

        public decimal? TradePerformanceMin
        {
            get
            {
                if (OpenRate == 0 || TickerMax == null || TickerMin == null) return null;
                var minPerf = 0m;
                if (PositionType == PositionType.Long)
                    minPerf = (TickerMin.Mid() - OpenRate) / OpenRate * 100m;
                else
                    minPerf = (OpenRate - TickerMax.Mid()) / OpenRate * 100m;

                return minPerf;
            }
        }
    }
}
