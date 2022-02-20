using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class EmaCross : BaseStrategy
    {
        public override string Name { get; set; } = "EMA Cross";
        public override int MinimumAmountOfCandles => 36;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var ema12 = candles.Ema(12);
            var ema26 = candles.Ema(26);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (ema12[i] < ema26[i] && ema12[i - 1] > ema26[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else if (ema12[i] > ema26[i] && ema12[i - 1] < ema26[i])
                    result.Add(TradeAdvice.Factory.Sell);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
