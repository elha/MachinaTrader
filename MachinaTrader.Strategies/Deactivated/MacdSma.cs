using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class MacdSma : BaseStrategy
    {
        public override string Name { get; set; } = "MACD SMA";
        public override int MinimumAmountOfCandles => 200;
        public override Period IdealPeriod => Period.Hour;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var macd = candles.Macd();
            var fastMa = candles.Sma(12);
            var slowMa = candles.Sma(26);
            var sma200 = candles.Sma(200);

            var closes = candles.Select(x => x.Close).ToList();
            
            for (int i = 0; i < candles.Count; i++)
            {
                if (i < 25)
                    result.Add(TradeAdvice.Factory.Hold);
                else if(slowMa[i] < sma200[i])
                    result.Add(TradeAdvice.Factory.Sell);
                else if (macd.Hist[i] >0 && macd.Hist[i-1] < 0 && macd.Macd[i] > 0 && fastMa[i] > slowMa[i] && closes[i-26] > sma200[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);

            }

            return result;
        }

    }
}
