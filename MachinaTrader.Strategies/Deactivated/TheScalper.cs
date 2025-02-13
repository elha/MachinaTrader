using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class TheScalper : BaseStrategy
    {
        public override string Name => "The Scalper";
        public override int MinimumAmountOfCandles => 200;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var stoch = candles.Stoch();
            var sma200 = candles.Sma(200);
            var closes = candles.Select(x => x.Close).ToList();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i < 1)
                    result.Add(TradeAdvice.Factory.Hold);
                else
                {
                    if (sma200[i] < closes[i] && 
                        stoch.K[i - 1] <= stoch.D[i - 1] &&
                        stoch.K[i] > stoch.D[i] &&
                        stoch.D[i - 1] < 20 &&
                        stoch.K[i - 1] < 20)
                        result.Add(TradeAdvice.Factory.Buy);
                    
                    else if (stoch.K[i - 1] <= stoch.D[i - 1] &&
                        stoch.K[i] > stoch.D[i] &&
                        stoch.D[i - 1] > 80 &&
                        stoch.K[i - 1] > 80)
                        result.Add(TradeAdvice.Factory.Sell);
                    
                    else
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

