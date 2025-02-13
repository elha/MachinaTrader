using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class Base150 : BaseStrategy
    {
        public override string Name { get; set; } = "Base 150";
        public override int MinimumAmountOfCandles => 365;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var sma6 = candles.Sma(6);
            var sma25 = candles.Sma(25);
            var sma150 = candles.Sma(150);
            var sma365 = candles.Sma(365);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                {
                    result.Add(TradeAdvice.Factory.Hold);
                }
                else
                {
                    if (sma6[i] > sma150[i]
                        && sma6[i] > sma365[i]
                        && sma25[i] > sma150[i]
                        && sma25[i] > sma365[i]
                        && (sma6[i - 1] < sma150[i]
                        || sma6[i - 1] < sma365[i]
                        || sma25[i - 1] < sma150[i]
                        || sma25[i - 1] < sma365[i]))
                        result.Add(TradeAdvice.Factory.Buy);
                    if (sma6[i] < sma150[i]
                        && sma6[i] < sma365[i]
                        && sma25[i] < sma150[i]
                        && sma25[i] < sma365[i]
                        && (sma6[i - 1] > sma150[i]
                        || sma6[i - 1] > sma365[i]
                        || sma25[i - 1] > sma150[i]
                        || sma25[i - 1] > sma365[i]))
                        result.Add(TradeAdvice.Factory.Sell);
                    else
                        result.Add(TradeAdvice.Factory.Hold);
                }
            }

            return result;
        }
    }
}
