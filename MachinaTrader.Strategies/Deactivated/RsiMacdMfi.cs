using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RsiMacdMfi : BaseStrategy
    {
        public override string Name { get; set; } = "RSI MACD MFI";
        public override int MinimumAmountOfCandles => 35;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var macd = candles.Macd(5, 10, 4);
            var rsi = candles.Rsi(16);
            var mfi = candles.Mfi();
            var ao = candles.AwesomeOscillator();

            var close = candles.Select(x => x.Close).ToList();

            for (int i = 0; i < candles.Count; i++)
            {
                if (mfi[i] < 30 && rsi[i] < 45 && ao[i] > 0)
                    result.Add(TradeAdvice.Factory.Buy);
                
                else if (mfi[i] > 30 && rsi[i] > 45 && ao[i] < 0)
                    result.Add(TradeAdvice.Factory.Sell);
                
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
