using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Indicators
{
    public static partial class Extensions
    {
        public static List<decimal?> Rsi(this List<Candle> source, int period = 14)
        {
            int outBegIdx, outNbElement;
            double[] rsiValues = new double[source.Count];

            var closes = source.Select(x => Convert.ToDouble(x.Close)).ToArray();

            var ema = TicTacTec.TA.Library.Core.Rsi(0, source.Count - 1, closes, period, out outBegIdx, out outNbElement, rsiValues);

            if (ema == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(rsiValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate RSI!");
        }

        public static List<decimal?> Rsi(this List<decimal> source, int period = 14)
        {
            int outBegIdx, outNbElement;
            double[] rsiValues = new double[source.Count];

            var sourceFix = source.Select(x => Convert.ToDouble(x)).ToArray();

            var ema = TicTacTec.TA.Library.Core.Rsi(0, source.Count - 1, sourceFix, period, out outBegIdx, out outNbElement, rsiValues);

            if (ema == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(rsiValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate RSI!");
        }

        public static List<Candle> SwitchSide(this List<Candle> source)
        {
            var max = source.Close().Max() * 1.8m;
            var result = new List<Candle>();
            foreach (var candle in source) result.Add(new Candle() {
                Close = max - candle.Close,
                High = max - candle.High,
                Low = max - candle.Low,
                Open = max - candle.Open,
                Timestamp = candle.Timestamp, Volume = candle.Volume });
            return result;
        }
    }
}
