using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Indicators
{
    public static partial class Extensions
    {
        public static List<decimal?> LinRegSlope(this List<Candle> source, int period = 18, CandleVariable type = CandleVariable.Close)
        {
            int outBegIdx, outNbElement;
            double[] slopeValues = new double[source.Count];
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

            var slope = TicTacTec.TA.Library.Core.LinearRegSlope(0, source.Count - 1, valuesToCheck, period, out outBegIdx, out outNbElement, slopeValues);

            if (slope == TicTacTec.TA.Library.Core.RetCode.Success)
            {
                return FixIndicatorOrdering(slopeValues.ToList(), outBegIdx, outNbElement);
            }

            throw new Exception("Could not calculate Linear Regression Slope!");
        }
                 
    }
}
