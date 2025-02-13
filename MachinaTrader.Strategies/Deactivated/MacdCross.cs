using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies.Simple
{
    public class MacdCross : BaseStrategy, INotificationTradingStrategy
    {
        public override string Name { get; set; } = "MACD X";
        public override int MinimumAmountOfCandles => 50;
        public override Period IdealPeriod => Period.Hour;

        public string BuyMessage => "MACD: *Oversold*\nTrend reversal to the *upside* is near.";
        public string SellMessage => "MACD: *Overbought*\nTrend reversal to the *downside* is near.";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var macd = candles.Macd();
            var crossUnder = macd.Macd.Crossunder(macd.Signal);
            var crossOver = macd.Macd.Crossover(macd.Signal);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);
                else if (macd.Macd[i] > 0 && crossUnder[i])
                    result.Add(TradeAdvice.Factory.Sell);
                else if (macd.Macd[i] < 0 && crossOver[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

    }
}
