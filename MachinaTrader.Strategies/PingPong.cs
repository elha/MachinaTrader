using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class PingPong : BaseStrategy
    {
        public override string Name => "PingPong";
        public override int MinimumAmountOfCandles => 2;
        public override Period IdealPeriod => Period.Minute;
        public string BuyMessage => "BuyTheDip: *Dip detected*\nTrend reversal to the *upside* is near.";
        public string SellMessage => "BuyTheDip: *Sell*\nTrend reversal to the *downside* is near.";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            
            for (int i = 0; i < candles.Count; i++)
            {
                if (i+1 == candles.Count)
                    if(System.DateTime.Now.Minute % 2 == 0)
                        result.Add(TradeAdvice.Buy);
                    else
                        result.Add(TradeAdvice.Sell);
                else
                    result.Add(TradeAdvice.Hold);
            }

            return result;
        }

        public override TradeAdvice Forecast(List<Candle> candles)
        {
            return Prepare(candles).LastOrDefault();
        }

        public override Candle GetSignalCandle(List<Candle> candles)
        {
            return candles.Last();
        }
    }
}

