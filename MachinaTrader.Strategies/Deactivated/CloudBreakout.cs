using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies.Simple
{
    public class CloudBreakout : BaseStrategy, INotificationTradingStrategy
    {
        public override string Name { get; set; } = "Cloud Breakout";
        public override int MinimumAmountOfCandles => 120;
        public override Period IdealPeriod => Period.Hour;

        public string BuyMessage => "Ichimoku: *Positive cloud break*\nTrend reversal to the *upside* is near.";
        public string SellMessage => "Ichimoku: *Negative cloud break*\nTrend reversal to the *downside* is near.";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            var ichiMoku = candles.Ichimoku();
            var close = candles.Close();

            var cloudBreakUpA = close.Crossover(ichiMoku.SenkouSpanA);
            var cloudBreakDownA = close.Crossunder(ichiMoku.SenkouSpanA);
            var cloudBreakUpB = close.Crossover(ichiMoku.SenkouSpanB);
            var cloudBreakDownB = close.Crossunder(ichiMoku.SenkouSpanB);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i == 0)
                    result.Add(TradeAdvice.Factory.Hold);

                // Upward cloud break from the bottom
                else if (ichiMoku.SenkouSpanA[i] > ichiMoku.SenkouSpanB[i] && cloudBreakUpB[i])
                    result.Add(TradeAdvice.Factory.Buy);
                else if (ichiMoku.SenkouSpanA[i] < ichiMoku.SenkouSpanB[i] && cloudBreakUpA[i])
                    result.Add(TradeAdvice.Factory.Buy);

                // Downward cloud break from the top
                else if (ichiMoku.SenkouSpanA[i] > ichiMoku.SenkouSpanB[i] && cloudBreakDownA[i])
                    result.Add(TradeAdvice.Factory.Sell);
                else if (ichiMoku.SenkouSpanA[i] < ichiMoku.SenkouSpanB[i] && cloudBreakDownB[i])
                    result.Add(TradeAdvice.Factory.Sell);
                
                else
                    result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }


    }
}
