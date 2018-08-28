using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Globals.Structure.Extensions
{
    public static class CandleExtensions
    {
        public static List<decimal> High(this List<Candle> source)
        {
            return source.Select(x => x.High).ToList();
        }

        public static List<decimal> Low(this List<Candle> source)
        {
            return source.Select(x => x.Low).ToList();
        }

        public static List<decimal> Open(this List<Candle> source)
        {
            return source.Select(x => x.Open).ToList();
        }

        public static List<decimal> Close(this List<Candle> source)
        {
            return source.Select(x => x.Close).ToList();
        }

        public static List<decimal> Hl2(this List<Candle> source)
        {
            return source.Select(x => (x.High + x.Low) / 2).ToList();
        }

        public static List<decimal> Hlc3(this List<Candle> source)
        {
            return source.Select(x => (x.High + x.Low + x.Close) / 3).ToList();
        }



        /// <summary>
        /// For candle data with inconsistent intervals 
        ///   (ie., for coins that don't have activity between two periods),
        ///   filling the gaps by extending the candle preceeding the gap until the next candle.
        /// This is usually more of an issue with low volume coins and shortewr time intervals.
        /// </summary>
        /// <param name="candles">Candle list containing time gaps.</param>
        /// <param name="period">Period of candle.</param>
        /// <returns></returns>
        public static async Task<List<Candle>> FillCandleGaps(this List<Candle> candles, Period period)
        {
            if (!candles.Any())
                return candles;

            // Candle response
            var filledCandles = new List<Candle>();
            var orderedCandles = candles.OrderBy(x => x.Timestamp).ToList();

            // Datetime variables
            DateTime nextTime;
            DateTime startDate = orderedCandles.First().Timestamp;
            DateTime endDate = DateTime.UtcNow;

            // Walk through the candles and fill any gaps
            for (int i = 0; i < orderedCandles.Count() - 1; i++)
            {
                var c1 = orderedCandles[i];
                var c2 = orderedCandles[i + 1];
                filledCandles.Add(c1);
                nextTime = c1.Timestamp.AddMinutes(period.ToMinutesEquivalent());
                while (nextTime < c2.Timestamp)
                {
                    var cNext = c1;
                    cNext.Timestamp = nextTime;
                    filledCandles.Add(cNext);
                    nextTime = cNext.Timestamp.AddMinutes(period.ToMinutesEquivalent());

                    //var backtestOptions = new BacktestOptions
                    //{
                    //    DataFolder = Global.DataPath,
                    //    Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                    //    Coins = new List<string>(new[] { coinsToBuy }),
                    //    CandlePeriod = Int32.Parse(candleSize)
                    //};
                    //await dataStore.SaveBacktestCandlesBulk(candles, Global.DataStoreBacktest);
                }
            }

            // Fill "extend" the last candle gap
            //var cLast = candles.Last();
            //filledCandles.Add(cLast);
            //nextTime = cLast.Timestamp.AddMinutes(period.ToMinutesEquivalent());
            //while (nextTime < endDate)
            //{
            //    var cNext = cLast;
            //    cNext.Timestamp = nextTime;
            //    filledCandles.Add(cNext);
            //    nextTime = cNext.Timestamp.AddMinutes(period.ToMinutesEquivalent());
            //}

            // Debugging. Don't need when running for real.
           // await Task.Delay(10);

            // Return the no-gap candles
            return filledCandles;
        }
    }
}
