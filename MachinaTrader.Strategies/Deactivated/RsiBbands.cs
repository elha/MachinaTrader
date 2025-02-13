using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RsiBbands : BaseStrategy
    {
        public override string Name { get; set; } = "RSI Bbands";
        public override int MinimumAmountOfCandles => 200;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var rsi = candles.Rsi(6);
            var bbands = candles.Bbands(200);
            var closes = candles.Select(x => x.Close).ToList();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i < 1)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (rsi[i - 1] > 50 && rsi[i] <= 50 && closes[i - 1] < bbands.UpperBand[i - 1] && closes[i] > bbands.UpperBand[i])
                    result.Add(TradeAdvice.Factory.Sell);
                else if (rsi[i - 1] < 50 && rsi[i] >= 50 && closes[i - 1] < bbands.LowerBand[i - 1] && closes[i] > bbands.LowerBand[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);

            }

            return result;
        }

    }
}
