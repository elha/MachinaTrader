using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RsiMacd : BaseStrategy
    {

        public override string Name { get; set; } = "RSI MACD";
        public override int MinimumAmountOfCandles => 52;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var macd = candles.Macd(24, 52, 18);
            var rsi = candles.Rsi(14);

            for (int i = 0; i < candles.Count; i++)
            {
                if (rsi[i] > 70 && (macd.Macd[i] - macd.Signal[i]) < 0)
                    result.Add(TradeAdvice.Factory.Sell);
                else if (rsi[i] < 30 && (macd.Macd[i] - macd.Signal[i]) > 0)
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
