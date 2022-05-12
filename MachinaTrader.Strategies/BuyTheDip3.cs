using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class BuyTheDip3 : BaseStrategy
    {
        public override string Name { get; set; } = "BuyTheDip3";
        public override int MinimumAmountOfCandles => 50;
        public override Period IdealPeriod => Period.Minute;

        public override string Parameters { get; set; } = "237";
        public override string MinParameters { get; set; } = "033";
        public override string MaxParameters { get; set; } = "499";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            var P1 = int.Parse(Parameters.Substring(0, 1));
            var P2 = int.Parse(Parameters.Substring(1, 1));
            var P3 = int.Parse(Parameters.Substring(2, 1));

            var BuyDrop = new decimal[] {0.02m, 0.15m, 0.3m, 0.5m,  0.7m,  0.9m,  1.1m }[P1]; // 1.3 Best

            var close = candles.Close();
            var emabuy = candles.Ema(P2).FillGaps();

            var SellRise = new decimal[] { 0.4m,  0.6m,  0.8m,  1.2m }[0]; // 0.8 Best

            var candlesSell = candles.SwitchSide();
            var closeSell = candlesSell.Close();
            var emasell = closeSell.Ema(P3).FillGaps();


            var statsBuy = 0;
            var statsSell = 0;
            //Candle lastBuy = null;
            for (int i = 0; i < candles.Count; i++)
            {
                if (i <= 40 || close[i] == 0)
                {
                    result.Add(TradeAdvice.Factory.Hold);
                    continue;
                }
                var bSignal = false;
                foreach (var k in new int[] { 18 })
                {
                    if (!bSignal && Check(BuyDrop, close, emabuy, i, k))  // 5 is best
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Buy,
                            Comment = $"drop {k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) < ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
                        });
                        statsBuy++;
                        //lastBuy = candles[i];
                        bSignal = true;
                    }
                }
                foreach (var k in new int[] { 9 })
                {
                    if (!bSignal && Check(SellRise, closeSell, emasell, i, k))  // 5 is best
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Sell,
                            Comment = $"rise {k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) > ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
                        });
                        statsSell++;
                        //lastBuy = null;
                        bSignal = true;
                    }
                }
                foreach (var k in new int[] { 28 })
                {
                    if (!bSignal && Check(SellRise, closeSell, emasell, i, k))
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Sell,
                            Comment = $"rise {k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) > ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
                        });
                        statsSell++;
                        //lastBuy = null;
                        bSignal = true;
                    }
                }

                if (!bSignal) result.Add(TradeAdvice.Factory.Hold);
            }

            return result;
        }

        private static bool Check(decimal Drop, List<decimal> targetclose, List<decimal> ema9, int i, int k)
        {
            int j;
            for (j = i-1; j > i-k; j--)
                if (!(ema9[j]>targetclose[j])) continue; // muss den gesamten Weg ][ unter ema9 liegen

            j -= 1;

            if (ema9[i] > targetclose[i]) return false;
            if (ema9[i-1] < targetclose[i-1]) return false;
            if (ema9[j] > targetclose[j]) return false;

            if ((((targetclose[i] - targetclose[j]) * 100m) / targetclose[j]) <= -Drop)
            {
                return true;
            }
            return false;

        }

    }
}

