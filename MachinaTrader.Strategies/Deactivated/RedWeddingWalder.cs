using System;
using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class RedWeddingWalder : BaseStrategy
    {
        public override string Name => "Red Wedding Walder";
        public override int MinimumAmountOfCandles => 100;
        public override Period IdealPeriod { get; } = Period.FourHours;

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
                var highs = candles.High();
                var opens = candles.Open();

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

                for (int i = 0; i < vClose.Count; i++)
                {
                    var prev_close = i == 0 ? null : vClose[i - 1];
                    var prev_open = i == 0 ? null : vOpen[i - 1];

                    if (prev_close == null && prev_open == null)
                        vOpen.Add(null);
                    else
                        vOpen.Add((prev_open == null ? prev_close : prev_open) + smooth * (prev_close - (prev_open == null ? prev_close : prev_open)));
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

                var fish = candles.Fisher(9);

                for (int i = 0; i < candles.Count; i++)
                {
                    if (i <= 2)
                        result.Add(TradeAdvice.Factory.Hold);
                    else if (fish[i] >= fish[i - 1] && fish[i - 1] >= fish[i - 2] && fish[i - 2] >= fish[i - 3] && closes[i] < snow_low[i] && opens[i] < snow_low[i])
                        result.Add(TradeAdvice.Factory.Buy);
                    else if (closes[i] > snow_high[i])
                        result.Add(TradeAdvice.Factory.Sell);
                    else
                        result.Add(TradeAdvice.Factory.Hold);
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
