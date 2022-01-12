using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Indicators
{
    public static partial class Extensions
    {
        public static List<decimal?> Dema(this List<Candle> source, int period = 9, CandleVariable type = CandleVariable.Close)
        {
            int outBegIdx, outNbElement;
            double[] demaValues = new double[source.Count];
            double[] valuesToCheck;

            switch (type)
            {
                case CandleVariable.Open:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Open)).ToArray();
                    break;
                case CandleVariable.Low:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Low)).ToArray();
                    break;
                case CandleVariable.High:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.High)).ToArray();
                    break;
                default:
                    valuesToCheck = source.Select(x => Convert.ToDouble(x.Close)).ToArray();
                    break;
            }

            var ema = TicTacTec.TA.Library.Core.Dema(0, source.Count - 1, valuesToCheck, period, out outBegIdx, out outNbElement, demaValues);

            if (ema == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(demaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate DEMA!");
        }

        public static List<decimal?> Dema(this List<decimal> source, int period = 30)
        {
            int outBegIdx, outNbElement;
            double[] demaValues = new double[source.Count];
            List<double?> outValues = new List<double?>();

            var sourceFix = source.Select(x => Convert.ToDouble(x)).ToArray();

            var sma = TicTacTec.TA.Library.Core.Dema(0, source.Count - 1, sourceFix, period, out outBegIdx, out outNbElement, demaValues);

            if (sma == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(demaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate DEMA!");
        }

        public static List<decimal?> Dema(this List<decimal?> source, int period = 30)
        {
            int outBegIdx, outNbElement;
            double[] demaValues = new double[source.Count];
            List<double?> outValues = new List<double?>();

            var sourceFix = source.Select(x => x.HasValue ? Convert.ToDouble(x) : 0).ToArray();

            var sma = TicTacTec.TA.Library.Core.Dema(0, source.Count - 1, sourceFix, period, out outBegIdx, out outNbElement, demaValues);

            if (sma == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(demaValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate DEMA!");
        }
    }
}
