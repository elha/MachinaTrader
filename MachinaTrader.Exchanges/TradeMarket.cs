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
using static MachinaTrader.Exchanges.MarketManager;

namespace MachinaTrader.Exchanges
{
    public class TradeMarket
    {
        public string Exchange;
        public string GlobalMarketName;
        public CurrencyPair CurrencyPair;
        public string SettleCurrency;
        public decimal Fee;


        // is enabled via allowed symbols
        public bool Active = false;

        // has enough candles to be traded
        public bool Filled = false;

        public Candle Last;

        // change to rate on last update
        public decimal LastRate = 1000000.0m;
        public decimal LastChange = 0m;
        public TradeAdvice LastStrategyAdvice = new TradeAdvice() { Advice = TradeAdviceEnum.Hold };
        
        public Ticker LastTicker
        {
            get
            {
                if (LastTickers.Count == 0) return null;
                return LastTickers.Last();
            }
            set
            {
                LastTickers.Add(value);
                if (LastTickers.Count > 10) LastTickers.RemoveAt(0);
            }
        }

        public List<Ticker> LastTickers { get; set; } = new List<Ticker>();

        public List<Candle> Candles;

        private static int mMaxCandles = 250;

        private static decimal mLockNewBuysIfOnePosIsBelow = -0.15m;
        private static decimal mLockNewBuysIfLastBuyIsYoungerThanMinutes = 2.5m;
        private static decimal mLockNewBuysIfMoreThanPositions = 3;

        private static decimal mDCAIfPosIsBelow = -0.5m;
        private static decimal mDCAAfterMinutes = 75m;

        public decimal LotSize { get; internal set; }
        public decimal QuoteToSettle { get; internal set; }

        // promille MA200-Change 10+15min
        public decimal Trend10 { get; internal set; }
        //public decimal Trend20 { get; internal set; }

        public TradeMarket()
        {
            Candles = new List<Candle>();
            Filled = false;
        }

        public static bool IsTradingTime(DateTime d)
        {
            d = d.ToUniversalTime();
            switch (d.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    return false;

                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                    return (3 <= d.Hour && d.Hour <= 16);

                case DayOfWeek.Friday:
                    return (3 <= d.Hour && d.Hour <= 13);
            }
            return false;
        }

        /// <summary>
        /// sell or buy when >=10
        /// </summary>
        /// <returns></returns>
        public (TradeAdvice, Trade) GetBuyAdvice(List<Trade> trades)
        {
            //if (MarketManager.GlobalTrend >= MarketManager.Trend.caution)
            //    return (TradeAdvice.Factory.Hold, null); // absolutely no deals if economy is drifting down

            //if (MarketTrend >= MarketManager.Trend.caution)
            //    return (TradeAdvice.Factory.Hold, null); // absolutely no deals if market is drifting down

            var Position = trades.FirstOrDefault(t => t.GlobalSymbol == GlobalMarketName);
            if (Position == null)
            {
                // new position only if sanity rules met

                if (trades.Any(t => t.TradePerformance <= mLockNewBuysIfOnePosIsBelow))
                    return (TradeAdvice.Factory.Hold, Position); // no deals if negative pos exists and is not very old

                if (trades.Count > 0 && (decimal)(DateTime.UtcNow - trades.Max(t => t.OpenDate)).TotalMinutes < mLockNewBuysIfLastBuyIsYoungerThanMinutes)
                    return (TradeAdvice.Factory.Hold, Position); // no deals if position opened recently

                if ((decimal)trades.Count > mLockNewBuysIfMoreThanPositions)
                    return (TradeAdvice.Factory.Hold, Position); // no deals if a lot of positions exist

                if (trades.Sum(t => t.StakeAmount) / DepotManager.PositionSize / 2.0m > mLockNewBuysIfMoreThanPositions)
                    return (TradeAdvice.Factory.Hold, Position); // no deals of capial is bound by existing positions

                //if (!IsTradingTime(Last.Timestamp))
                //    return (TradeAdvice.Factory.Hold, Position);  // no deals in off-hours

                //if ((MarketManager.GetUSD(CurrencyPair.BaseCurrency, AvgVolume) / DepotManager.PositionSize) < 50)
                //    return (TradeAdvice.Factory.Hold, Position); // low volume is bad

                // open short
                if (LastStrategyAdvice.Advice == TradeAdviceEnum.Buy && MarketTrend == Trend.up
                    && MarketManager.GlobalTrendScore >= +25m)
                    return (TradeAdvice.Factory.Sell, Position);

                // open long
                if (LastStrategyAdvice.Advice == TradeAdviceEnum.Sell && MarketTrend == Trend.down
                    && MarketManager.GlobalTrendScore <= -25m)
                    return (TradeAdvice.Factory.Buy, Position);

                return (TradeAdvice.Factory.Hold, Position); // no deals if in wrong phase
            }

            if (Position.IsBuying || Position.IsSelling)
            {
                // dont touch
                return (TradeAdvice.Factory.Hold, Position);
            }
            else 
            {
                // check dca
                //var age = (decimal)(DateTime.UtcNow - Position.DcaDate.GetValueOrDefault(Position.OpenDate)).TotalMinutes;
                //var advice = LastStrategyAdvice;
                //if (advice.Advice == TradeAdviceEnum.Buy && age > mDCAAfterMinutes && Position.TradePerformance < mDCAIfPosIsBelow)
                //{
                //    return (new TradeAdvice()
                //    {
                //        Advice = TradeAdviceEnum.Buy,
                //        Comment = $"dca {age:N2} {Position.TradePerformance:N2}% < {mDCAIfPosIsBelow}% { Position.TickerLast.Mid():N2} < { Position.OpenRate:N2} @ { Position.OpenDate.ToString("HH:mm:ss")}  DcaDate { Position.DcaDate?.ToString("HH:mm:ss")}"
                //    }, Position);
                //}

                return (TradeAdvice.Factory.Hold, Position);
            }
        }

        internal string GetTrendInfo()
        {
            var gain = (Last.Close - LastTrendQuote) / Last.Close * 1000m;
            var age = (DateTime.UtcNow - LastTrend).TotalMinutes;
            return $"{MarketTrend}:{gain:N1}‰/{age:N0}";
        }

        public Trend MarketTrend;
        public DateTime LastTrend = DateTime.UtcNow;
        public decimal LastTrendQuote = 0m;

        public void CalcTrend()
        {
            Trend newTrend;
            if (Trend10 <= -0.3m)
                newTrend = MarketManager.Trend.down;
            else if (Trend10 <= +0.3m)
                newTrend = MarketManager.Trend.side;
            else
                newTrend = MarketManager.Trend.up;

            if(MarketTrend!=newTrend)
            {
                MarketTrend = newTrend;
                LastTrend = DateTime.UtcNow;
                LastTrendQuote = Last.Close;
            }
        }

        public TradeAdvice GetSellAdvice(Trade Position)
        {
            //if (MarketManager.GlobalTrend == MarketManager.Trend.down)
            //    return new TradeAdvice()
            //    {
            //        Advice = TradeAdviceEnum.Sell,
            //        SellType = SellType.Immediate,
            //        Comment = $"killglobaltrend {Position.TradePerformance:N2}%  { Position.TickerLast.Mid():N2} - { Position.OpenRate:N2} @ { Position.OpenDate.ToString("HH:mm:ss")}"
            //    }; // sell off if economy is drifting down

            //if (MarketTrend == MarketManager.Trend.down)
            //    return new TradeAdvice()
            //    {
            //        Advice = TradeAdviceEnum.Sell,
            //        SellType = SellType.Immediate,
            //        Comment = $"killmarkettrend {Position.TradePerformance:N2}%  { Position.TickerLast.Mid():N2} - { Position.OpenRate:N2} @ { Position.OpenDate.ToString("HH:mm:ss")}"
            //    }; // sell off if market is drifting down

            //if (MarketManager.GlobalTrend == Trend.caution && Position.PositionType== PositionType.Long)
            //    PlannedSellPerformance = -8.0m; // sell off
            //else
            //if (MarketTrend == MarketManager.Trend.up)
            //    PlannedSellPerformance *= 1.5m; //up
            //else
            //    PlannedSellPerformance *= 0.3m; //side

            var bReverseChange = (Position.PositionType == PositionType.Long) ? (LastChange < 0m) : (LastChange > 0m);

            if (Position.TradePerformanceMax.HasValue && Position.TradePerformanceMax > Position.SellOnPercentage && bReverseChange)
            {
                // straight sell if result is over plan and rate is falling
                return new TradeAdvice()
                {
                    Advice = (Position.PositionType == PositionType.Long) ? TradeAdviceEnum.Sell: TradeAdviceEnum.Buy,
                    SellType = SellType.TrailingStopLoss,
                    Comment = $"topperf {Position.TradePerformance:N2}% > {Position.SellOnPercentage:N2}% { Position.TickerLast.Mid():N2} > { Position.OpenRate:N2} @ { Position.OpenDate.ToString("HH:mm:ss")}"
                };
            }
            else if (Position.TradePerformance < Position.SellOnPercentage)
            {
                // block sell if result below plan
                return new TradeAdvice() { Advice = TradeAdviceEnum.Hold };
            }
            else
            {
                return LastStrategyAdvice;
            }
        }

        internal void Update(MarketSummary market, bool tradeable)
        {
            Active = tradeable;

            QuoteToSettle = market.QuoteToSettle;

            LastChange = market.Mid() - LastRate;
            LastRate = market.Mid();

            var bNewTick = Last == null || Last?.Timestamp.Minute != DateTime.UtcNow.Minute;
            if (bNewTick)
            {
                Last = new Candle()
                {
                    Timestamp = DateTime.UtcNow.RoundDown(TimeSpan.FromMinutes((int)Period.Minute)),
                    Open = (Last?.Close).GetValueOrDefault(market.Mid())
                };
                Last.High = Last.Open;
                Last.Low = Last.Open;
            }

            Last.Close = market.Mid();
            Last.High = Math.Max(Last.Close, Last.High);
            Last.Low = Math.Min(Last.Close, Last.Low);
            Last.Volume = market.Volume;


            LastTicker = new Ticker() { Last = market.Last, Volume = market.Volume, Ask = market.Ask, Bid = market.Bid };

            if (bNewTick)
            {
                Candles.Add(Last);
                while (Candles.Count < mMaxCandles) Candles.Add(Last); // fill up initial
                if (Candles.Count > mMaxCandles) Candles.RemoveAt(0);
            }

            // 15min-change more than 0.1% down
            var ema200 = Candles.Ema(200).FillGaps();
            Trend10 = (ema200[Candles.Count - 1] - ema200[Candles.Count - 11]) / Last.Close * 1000m;
            //Trend20 = (ema200[Candles.Count - 1] - ema200[Candles.Count - 21]) / Last.Close * 1000m;

            CalcTrend();

            var Strategy = MarketTrend == Trend.up ?
                MarketManager.StrategyUp : MarketManager.StrategySide;

            if (Active && Strategy != null)
            {
                LastStrategyAdvice = Strategy.Prepare(Candles).Last();
                LastStrategyAdvice.Strategy = Strategy.Name + ":" + Strategy.Parameters;
            }
            else
                LastStrategyAdvice = new TradeAdvice() { Advice = TradeAdviceEnum.Hold };
        }

        internal async Task SaveToDB()
        {
            BacktestOptions backtestOptions = new BacktestOptions() { CandlePeriod = (int)Period.Minute, Exchange = Globals.Structure.Enums.Exchange.Binance, Coin = GlobalMarketName };
            await Global.DataStoreBacktest.SaveBacktestCandlesBulkCheckExisting(new Candle[] { Last }.ToList(), backtestOptions);
        }

    }

}
