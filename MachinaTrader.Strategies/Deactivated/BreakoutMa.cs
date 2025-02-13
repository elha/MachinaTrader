using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class BreakoutMa : BaseStrategy
    {
        public override string Name => "Breakout MA";
        public override int MinimumAmountOfCandles => 35;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var sma20 = candles.Sma(20, CandleVariable.Low);
            var ema34 = candles.Ema(34);
            var adx = candles.Adx(13);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (ema34[i - 1] > sma20[i - 1] && ema34[i] < sma20[i] && adx[i] > 25)
                    result.Add(TradeAdvice.Factory.Buy);
                else if (ema34[i] > sma20[i] && ema34[i-1] < sma20[i-1] && adx[i] > 25)
                    result.Add(TradeAdvice.Factory.Sell);
                else
                    result.Add(TradeAdvice.Factory.Hold);
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
