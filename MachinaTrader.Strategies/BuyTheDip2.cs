using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class BuyTheDip2 : BaseStrategy
    {
        public override string Name { get; set; } = "BuyTheDip2";
        public override int MinimumAmountOfCandles => 50;
        public override Period IdealPeriod => Period.Minute;

        public override string Parameters { get; set; } = "30432";
        public override string MinParameters { get; set; } = "00222";
        public override string MaxParameters { get; set; } = "43555";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            var P1 = int.Parse(Parameters.Substring(0, 1));
            var P2 = int.Parse(Parameters.Substring(1, 1));
            var P3 = int.Parse(Parameters.Substring(2, 1));
            var P4 = int.Parse(Parameters.Substring(3, 1));
            var P5 = int.Parse(Parameters.Substring(4, 1));

            var BuyDrop = new decimal[] { 0.5m,  0.7m,  0.9m,  1.1m,  1.3m }[P1]; // 1.3 Best

            var close = candles.Close();
            var macdBuy = candles.Macd(14, 18, 20).Hist.Rises();


            var SellRise = new decimal[] { 0.4m,  0.6m,  0.8m,  1.2m }[P2]; // 0.8 Best

            var canclesSell = candles.SwitchSide();
            var closeSell = canclesSell.Close();
            var macdSell = canclesSell.Macd(14, 18, 20).Hist.Rises();


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
                foreach (var k in new int[] { 5, 9 })  // 28 is best
                {
                    if (!bSignal && Check(BuyDrop, close, macdBuy, i, k, P3))  // 5 is best
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Buy,
                            Comment = $"drop {P3}/{k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) < ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
                        });
                        statsBuy++;
                        //lastBuy = candles[i];
                        bSignal = true;
                    }
                }
                foreach (var k in new int[] { 5, 9 })
                {
                    if (!bSignal && Check(SellRise, closeSell, macdSell, i, k, P4))  // 5 is best
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Sell,
                            Comment = $"rise {P4}/{k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) > ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
                        });
                        statsSell++;
                        //lastBuy = null;
                        bSignal = true;
                    }
                }
                foreach (var k in new int[] { 18, 28 })
                {
                    if (!bSignal && Check(SellRise, closeSell, macdSell, i, k, P5))
                    {
                        result.Add(new TradeAdvice()
                        {
                            Advice = TradeAdviceEnum.Sell,
                            Comment = $"rise {P5}/{k} {((candles[i].Close - candles[i - k].Close) * 100m) / candles[i - k].Close}% ({candles[i].Close} @ {candles[i].Timestamp.ToString("HH:mm:ss")}) > ({candles[i - k].Close} @ {candles[i - k].Timestamp.ToString("HH:mm:ss")})"
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

        private static bool Check(decimal Drop, List<decimal> targetclose, List<bool> targetmacd, int i, int k, int macdCount)
        {
            if ((((targetclose[i] - targetclose[i - k]) * 100m) / targetclose[i - k]) <= -Drop)
           
            {
                for(int j = 0; j < macdCount; j++)
                    if(!targetmacd[i - j]) return false;

                return true;
            }
            return false;

        }

    }
}

