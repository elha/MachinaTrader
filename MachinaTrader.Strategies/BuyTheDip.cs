using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class BuyTheDip : BaseStrategy
    {
        public override string Name { get; set; } = "BuyTheDip";
        public override int MinimumAmountOfCandles => 150;
        public override Period IdealPeriod => Period.Minute;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            var BuyDrop = 1.0m; //%

            var close = candles.Close();

            var macdBuy = candles.Macd(14, 18, 20).Hist.Rises();

            var statsBuy = 0;
            var statsSell = 0;
            Candle lastBuy = null;
            for (int i = 0; i < candles.Count; i++)
            {
                if (i <= 30 || close[i] == 0)
                    result.Add(TradeAdvice.Factory.Hold);

                else if (lastBuy == null)
                {
                    // detect dip with stable plateau
                    // dip by more than x% in x min
                    // and macdBuy rising
                    if ((((close[i] - close[i - 20]) * 100m) / close[i - 20]) <= -BuyDrop
                        && macdBuy[i] && macdBuy[i - 1])
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Buy,
                            Comment = $"drop20 {((close[i] - close[i - 20]) * 100m) / close[i - 20]}% ({close[i]} @ {candles[i].Timestamp}) < ({close[i - 20]} @ {candles[i - 20].Timestamp})"
                        });
                        statsBuy++;
                        lastBuy = candles[i];
                    }
                    else if ((((close[i] - close[i - 30]) * 100m) / close[i - 30]) <= -BuyDrop
                        && macdBuy[i] && macdBuy[i - 1])
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Buy,
                            Comment = $"drop30 {((close[i] - close[i - 30]) * 100m) / close[i - 30]}% ({close[i]} @ {candles[i].Timestamp}) < ({close[i - 30]} @ {candles[i - 30].Timestamp})"
                        });
                        statsBuy++;
                        lastBuy = candles[i];
                    }
                    else
                    {
                        result.Add(TradeAdvice.Factory.Hold);
                    }
                }
                else
                {
                    // already bought, plan sell within next 90 Minutes
                    // Downward Tunnel from BuyDrop down to 0
                    decimal ticks = (decimal)(candles[i].Timestamp - lastBuy.Timestamp).TotalMinutes;
                    var gain = (BuyDrop / 100m) * lastBuy.Close * 0.80m;
                    var knockout = lastBuy.Close + gain;


                    if (close[i] > knockout)
                    {
                        result.Add(new TradeAdvice() { Advice = TradeAdviceEnum.Sell, Comment = $"Knockout on {ticks}" });
                        statsSell++;
                        lastBuy = null;
                    }
                    else if (ticks >= 4 * 24 * 60)
                    {
                        result.Add(new TradeAdvice() { Advice = TradeAdviceEnum.Sell, Comment = $"Force on {ticks}" });
                        statsSell++;
                        lastBuy = null;
                    }
                    else
                    {
                        result.Add(TradeAdvice.Factory.Hold);
                    }
                }

            }

            return result;
        }

    }
}
