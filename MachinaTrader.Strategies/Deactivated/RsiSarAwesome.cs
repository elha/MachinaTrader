using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RsiSarAwesome : BaseStrategy
    {
        public override string Name => "RSI SAR Awesome";
        public override int MinimumAmountOfCandles => 35;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var sar = candles.Sar();
            var rsi = candles.Rsi(5);
            var ao = candles.AwesomeOscillator();

            var close = candles.Select(x => x.Close).ToList();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i >= 2)
                {
                    var currentSar = sar[i];
                    var priorSar = sar[i - 1];
                    
                    if (currentSar < close[i] && priorSar > close[i] && ao[i] > 0 && rsi[i] > 50)
                        result.Add(TradeAdvice.Factory.Buy);
                    
                    else if (currentSar > close[i] && priorSar < close[i] && ao[i] < 0 && rsi[i] < 50)
                        result.Add(TradeAdvice.Factory.Sell);
                    
                    else
                        result.Add(TradeAdvice.Factory.Hold);
                }
                else
                {
                    result.Add(TradeAdvice.Factory.Hold);
                }
            }

            return result;
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }

        public override TradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }
    }
}
