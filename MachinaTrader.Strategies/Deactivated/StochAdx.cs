using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{

    public class StochAdx : BaseStrategy
    {
        public override string Name { get; set; } = "Stoch ADX";
        public override int MinimumAmountOfCandles => 15;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var stoch = candles.Stoch(13);
            var adx = candles.Adx(14);
            var bearBull = candles.BearBull();

            for (int i = 0; i < candles.Count; i++)
            {
                if (adx[i] > 50 && (stoch.K[i] > 90 || stoch.D[i] > 90) && bearBull[i] == -1)
                    result.Add(TradeAdvice.Factory.Sell);
                else if (adx[i] < 20 && (stoch.K[i] < 10 || stoch.D[i] < 10) && bearBull[i] == 1)
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
