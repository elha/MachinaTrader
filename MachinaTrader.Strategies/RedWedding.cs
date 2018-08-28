using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RedWedding : BaseStrategy
    {
        public override string Name => "Red Wedding";
        public override int MinimumAmountOfCandles => 100;
        public override Period IdealPeriod => Period.FourHours;

        private decimal _lastValue = 0;

        public string BuyMessage => $"Positive change - Trend reversal to the *upside* is near.";
        public string SellMessage => $"Negative change - Trend reversal to the *downside* is near.";

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();

            try
            {
                var fastEnd = 0.666m;

                var open = candles.Open().Sma(6);
                var close = candles.Close().Sma(6);
                var high = candles.High().Sma(6);
                var low = candles.Low().Sma(6);

                var closes = candles.Close();

                // Calculate the vClose
                var vClose = new List<decimal?>();
                for (int i = 0; i < open.Count; i++)
                {
                    if (open[i].HasValue && close[i].HasValue && low[i].HasValue && high[i].HasValue)
                        vClose.Add((open[i].Value + close[i].Value + low[i].Value + high[i].Value) / 4);
                    else
                        vClose.Add(null);
                }

                // Calculate the vOpen
                var smooth = fastEnd * fastEnd;
                var vOpen = new List<decimal?>();
                var shiftedvClose = new List<decimal?> { null };

                foreach (var item in vClose)
                    if (item != vClose.Last())
                        shiftedvClose.Add(item);

                for (int i = 0; i < vClose.Count; i++)
                {
                    if (shiftedvClose[i] != null)
                    {
                        if (vClose[i] == null)
                            vOpen.Add(shiftedvClose[i]);
                        else if (vOpen[i - 1] == null)
                            vOpen.Add(shiftedvClose[i]);
                        else
                            vOpen.Add(vOpen[i - 1] + smooth * (shiftedvClose[i] - vOpen[i - 1]));
                    }
                }

                var snow_high = new List<decimal?>();

                for (int i = 0; i < vClose.Count; i++)
                    if (high[i].HasValue && vClose[i].HasValue && vOpen[i].HasValue)
                        snow_high.Add(Math.Max(high[i].Value, Math.Max(vClose[i].Value, vOpen[i].Value)));
                    else
                        snow_high.Add(null);

                var snow_low = new List<decimal?>();
                for (int i = 0; i < vClose.Count; i++)
                    if (low[i].HasValue && vClose[i].HasValue && vOpen[i].HasValue)
                        snow_low.Add(Math.Min(low[i].Value, Math.Min(vClose[i].Value, vOpen[i].Value)));
                    else
                        snow_low.Add(null);

                var long_sma = vClose.Sma(10);
                var short_sma = vClose.Sma(3);
                var stoch = candles.Stoch();
                var fish = candles.Fisher();

                var sma_crossover = short_sma.Crossover(long_sma);
                var sma_crossunder = short_sma.Crossunder(long_sma);
                var snow_cross = vClose.Crossunder(vOpen);

                var stoch_cross = stoch.K.Crossunder(80);
                var stoch_cross2 = stoch.K.Crossunder(stoch.D);

                for (int i = 0; i < candles.Count; i++)
                {
                    if (i <= 1)
                        result.Add(TradeAdvice.Hold);
                    else if (fish[i] >= fish[i - 1] && closes[i] < snow_high[i] && sma_crossover[i])
                        result.Add(TradeAdvice.Buy);
                    else if ((fish[i] < fish[i - 1] && fish[i - 1] >= fish[i - 2]) || sma_crossunder[i] || snow_cross[i] || stoch_cross[i] || (stoch_cross2[i] && stoch.K[i - 1] > 80))
                        result.Add(TradeAdvice.Sell);
                    else
                        result.Add(TradeAdvice.Hold);
                }

                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
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
