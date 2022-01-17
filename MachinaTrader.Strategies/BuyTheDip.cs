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
        public override string Name => "BuyTheDip";
        public override int MinimumAmountOfCandles => 60;
        public override Period IdealPeriod => Period.Minute;
        public string BuyMessage => "BuyTheDip: *Dip detected*\nTrend reversal to the *upside* is near.";
        public string SellMessage => "BuyTheDip: *Sell*\nTrend reversal to the *downside* is near.";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();
            var BuyDrop = 3.5m; //%

            var close = candles.Close();

            var ma200 = candles.Ema(200);
            var macdBuy = candles.Macd(4, 12, 20).Hist.Rises();

            var macdSell = candles.Macd();
            var crossUnder = macdSell.Macd.Crossunder(macdSell.Signal);

            var rsi = candles.Rsi(20);
            var crossOver = rsi.Crossover(30);
            var crossUnderRSI = rsi.Crossunder(70);

            for (int i = 0; i < candles.Count; i++)
            {
                if (i <= 50 || close[i] == 0)
                    result.Add(TradeAdvice.Hold);

                // detect dip with stable plateau
                // dip by more than 1,2% in 20 min
                // and macdBuy rising
                else if ((((close[i] - close[i - 20]) * 100m) / close[i - 20]) <= -BuyDrop
                    && macdBuy[i])

                    result.Add(TradeAdvice.Buy);
                else if ((((close[i] - close[i - 30]) * 100m) / close[i - 30]) <= -BuyDrop
                    && macdBuy[i])

                    result.Add(TradeAdvice.Buy);
                else if ((((close[i] - close[i - 40]) * 100m) / close[i - 40]) <= -BuyDrop
                    && macdBuy[i])

                    result.Add(TradeAdvice.Buy);
                else if (crossUnderRSI[i])
                    result.Add(TradeAdvice.Sell);
                // Downward cloud break from the top
                //else if ((((close[i] - close[i - 50]) * 100m) / close[i - 50]) >= 2*BuyDrop
                //    && !macdBuy[i])
                //    result.Add(TradeAdvice.Sell);
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

