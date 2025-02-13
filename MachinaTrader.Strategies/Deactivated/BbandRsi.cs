using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class BbandRsi : BaseStrategy
    {
        public override string Name { get; set; } = "BBand RSI";
        public override int MinimumAmountOfCandles => 20;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var currentPrices = candles.Select(x => x.Close).ToList();
            var bbands = candles.Bbands(20);
            var rsi = candles.Rsi(16);
            
            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (rsi[i] < 30 && currentPrices[i] < bbands.LowerBand[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else if (rsi[i] > 70)
                    result.Add(TradeAdvice.Factory.Sell);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
