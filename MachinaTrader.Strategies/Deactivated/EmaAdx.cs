using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class EmaAdx : BaseStrategy
    {
        public override string Name { get; set; } = "EMA ADX"; 
        public override int MinimumAmountOfCandles => 36;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var emaFast = candles.Ema(12);
            var emaShort = candles.Ema(36);
            var adx = candles.Adx();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (emaFast[i] > emaShort[i] && emaFast[i - 1] < emaShort[i] && adx[i] < 20)
                    result.Add(TradeAdvice.Factory.Sell);
                else if (emaFast[i] < emaShort[i] && emaFast[i - 1] > emaShort[i] && adx[i] >= 20)
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
