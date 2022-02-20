using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Exchanges
{
    public class TradeMarket
    {
        public string GlobalMarketName;
        public CurrencyPair CurrencyPair;
        public string SettleCurrency;
        public decimal Fee;


        // is enabled via allowed symbols
        public bool Active = false;

        // has enough candles to be traded
        public bool Filled = false;

        public Candle Last;

        public Ticker LastTicker;
        public List<Candle> Candles;


        public List<decimal> mVolume;
        public decimal AvgVolume;

        private static int mMaxCandles = 250;
        private static int mVolumeTicks = 20;

        public decimal LotSize { get; internal set; }

        public TradeMarket()
        {
            Candles = new List<Candle>();
            mVolume = new List<decimal>();
            Filled = false;
        }

        internal bool IsTradingTime(DateTime d)
        {
            d = d.ToUniversalTime();
            switch (d.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                    return !(0 <= d.Hour && d.Hour <= 6) && !(11 <= d.Hour && d.Hour <= 12) && !(17 <= d.Hour && d.Hour <= 18);

                case DayOfWeek.Friday:
                    return !(14 <= d.Hour && d.Hour <= 23);
            }
            return false;
        }

        /// <summary>
        /// sell or buy when >=10
        /// </summary>
        /// <returns></returns>
        public TradeAdvice GetBuyAdvice()
        {
            if (!IsTradingTime(Last.Timestamp)) return TradeAdvice.Factory.Hold;
            if ((MarketManager.GetUSD(CurrencyPair.BaseCurrency, AvgVolume) / DepotManager.PositionSize) < 50) return TradeAdvice.Factory.Hold; // low volume is bad

            return MarketManager.Strategy.Prepare(Candles).Last();

                //if (MACD[MACD.Count - 1]) score += 4; // MACD Rising
                //if (MACD[MACD.Count - 2]) score += 4;

                //if ((Last.Close / EMA200[EMA200.Count - 1]) < 0.98m) score += 3; // buy if way below ema200
                //if ((Last.Close / EMA200[EMA200.Count - 1]) < 0.99m) score += 2;

                //if (Math.Abs(LastChange) < 0.002m) score += 1; // sell if calm
                //if (Math.Abs(Last2Change) < 0.002m) score += 1; // sell if calm
                //if (Last10Change < -0.015m) score += 3; // sell if was bigger downtrend
                //if (Last15Change < -0.025m) score += 3; // sell if was bigger downtrend


        }

        public TradeAdvice GetSellAdvice(Trade Position)
        {
            var advice = MarketManager.Strategy.Prepare(Candles).Last();

            if (advice.Advice != TradeAdviceEnum.Sell)
            {
                var gain = (Last.Close - Position.OpenRate) / Position.OpenRate * 100m;
                var age = (decimal)(DateTime.Now - Position.OpenDate).TotalMinutes;
                if (age > 45)
                {
                    var target = 1.0m - ((age - 45) * 0.1m);
                    if (gain>target)
                    {
                        advice.Advice = TradeAdviceEnum.Sell;
                        advice.Comment = $"aged {age} {gain}% > {target}% { Last.Close } > { Position.OpenRate } @ { Position.OpenDate.ToString("HH:mm:ss")}";
                    }
                }
                else
                {
                    var target = 2.0m;
                    if (gain > target)
                    {
                        advice.Advice = TradeAdviceEnum.Sell;
                        advice.Comment = $"gained {age} {gain}% > {target}% { Last.Close } > { Position.OpenRate } @ { Position.OpenDate.ToString("HH:mm:ss")}";
                    }
                }
            }

            return advice;

                //if (!MACD[MACD.Count - 1]) score += 4; // MACD falling
                //if (!MACD[MACD.Count - 2]) score += 4;

                //if ((Last.Close / Position.OpenRate) > 1.05m) score += 12; // sell if good results
                //if ((Last.Close / Position.OpenRate) > 1.03m) score += 8;
                //if ((Last.Close / Position.OpenRate) > 1.01m) score += 4;

                //if ((Last.Close / Position.OpenRate) < 0.985m) score += 10; // stop loss

                //if ((Last.Timestamp - Position.OpenDate).TotalHours > 5) score += 1; // no old things
                //if ((Last.Timestamp - Position.OpenDate).TotalHours > 4) score += 1;
                //if ((Last.Timestamp - Position.OpenDate).TotalHours > 3) score += 1;
                //if ((Last.Timestamp - Position.OpenDate).TotalHours > 2) score += 1;
                //if ((Last.Timestamp - Position.OpenDate).TotalHours > 1) score += 1;

                //if (!IsTradingTime(Last.Timestamp, 0)) score += 10; // sell off when eob approaching
                //if (!IsTradingTime(Last.Timestamp, 0.2)) score += 2;
                //if (!IsTradingTime(Last.Timestamp, 0.4)) score += 2;
                //if (!IsTradingTime(Last.Timestamp, 0.6)) score += 2;
                //if (!IsTradingTime(Last.Timestamp, 0.8)) score += 1;
                //if (!IsTradingTime(Last.Timestamp, 1.0)) score += 1;
        }

        internal void Update(MarketSummary market, bool tradeable)
        {
            if (Last?.Timestamp.Minute == DateTime.UtcNow.Minute) return;


            Active = tradeable;

            Last = new Candle() { Timestamp = DateTime.Now.RoundDown(TimeSpan.FromMinutes((int)Period.Minute)), Close = market.Mid(), Volume = market.Volume };
            if (Candles.Count == 0)
                Last.Open = Last.Close;
            else
                Last.Open = Candles.Last().Close;
            Last.High = Math.Max(Last.Close, Last.Open);
            Last.Low = Math.Min(Last.Close, Last.Open);

            LastTicker = new Ticker() { Last = market.Last, Volume = market.Volume, Ask = market.Ask, Bid = market.Bid };

            CalcIndicators();

        }

        internal async Task SaveToDB()
        {
            BacktestOptions backtestOptions = new BacktestOptions() { CandlePeriod = (int)Period.Minute, Exchange = Exchange.Binance, Coin = GlobalMarketName };
            await Global.DataStoreBacktest.SaveBacktestCandlesBulkCheckExisting(new Candle[] { Last }.ToList(), backtestOptions);
        }

        /// <summary>
        /// only for backtesting
        /// </summary>
        /// <param name="candles"></param>
        public void Update(Candle candle)
        {
            Active = true;

            Last = candle;
            LastTicker = new Ticker() { Last = Last.Close, Volume = Last.Volume, Ask = Last.Close, Bid = Last.Close };

            CalcIndicators();
        }

        private void CalcIndicators()
        {
            mVolume.Add(Last.Volume);
            if (mVolume.Count > mVolumeTicks) mVolume.RemoveAt(0);
            AvgVolume = mVolume.Median();

            Candles.Add(Last);
            while (Candles.Count < mMaxCandles) Candles.Add(Last); // fill up initial
            if (Candles.Count > mMaxCandles) Candles.RemoveAt(0);

        }
    }

}
