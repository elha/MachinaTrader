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
        public override string Name { get; set; } = "PingPong";
        public override int MinimumAmountOfCandles => 2;
        public override Period IdealPeriod => Period.Minute;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            
            for (int i = 0; i < candles.Count; i++)
            {
                if (candles[i].Timestamp.Minute == 1)
                    result.Add(TradeAdvice.Factory.Buy);
                else if (candles[i].Timestamp.Minute == 45)
                    result.Add(TradeAdvice.Factory.Sell);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}

