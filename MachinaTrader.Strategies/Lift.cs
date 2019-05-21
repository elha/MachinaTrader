using System.Collections.Generic;
using System.Linq;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Indicators;

namespace MachinaTrader.Strategies
{
    public class Lift : BaseStrategy
    {
        public override string Name => "Lift";
        public override int MinimumAmountOfCandles => 14;
        public override Period IdealPeriod => Period.Minute;

        public override List<TradeAdvice> Prepare(List<Candle> candles)
        {
            var result = new List<TradeAdvice>();


            decimal TopBand = 80;
            decimal BottomBand = 20;


            var k = candles.StochRsi(fastKPeriod: 3).K;
            var d = candles.StochRsi(fastKPeriod: 3).D;

            var k_crossover = k.Crossover(BottomBand);
            var d_crossunder = d.Crossunder(TopBand);

            //var linreg = candles.LinReg(18);

            //var atr = candles.Atr(6);
            //int atr_Scale = 1;
           
            //var lrslope = candles.LinRegSlope(18);

            //var ema = candles.Ema(6);
            //var sma_short = candles.Sma(6);
            //var sma_long = candles.Sma(24);
                        
            //var closes = candles.Select(x => x.Close).ToList();
            //var opens = candles.Select(x => x.Open).ToList();

            for (int i = 0; i < candles.Count; i++)
            {
                if (i < 1)
                    result.Add(TradeAdvice.Hold);
                else
                {
                    if ( k_crossover[i] )

                        //((opens[i] + closes[i]) / 2) - ((opens[i-1] + closes[i-1]) / 2) > 0 &&
                        //    ((opens[i] + closes[i]) / 2) > (((linreg[i] + atr_Scale * atr[i]) + (linreg[i] - atr_Scale * atr[i])) / 2) &&
                        //    (((linreg[i] + atr_Scale * atr[i]) + (linreg[i] - atr_Scale * atr[i])) / 2) > ema[i] &&
                        //     (((linreg[i-1] + atr_Scale * atr[i-1]) + (linreg[i-1] - atr_Scale * atr[i-1])) / 2) > ema[i-1] &&
                        //     ema[i] > sma_short[i] &&
                        //     sma_short[i] > sma_long[i] &&
                        //     lrslope[i] > 0 &&
                        //     sma_short[i] - sma_short[i-1] > 0 )

                        result.Add(TradeAdvice.Buy);

                    else if ( d_crossunder[i] )
                        //(((linreg[i] + atr_Scale * atr[i]) + (linreg[i] - atr_Scale * atr[i])) / 2) < ema[i] || ema[i] < sma_short[i])
                    
                        result.Add(TradeAdvice.Sell);
                    
                    else

                        result.Add(TradeAdvice.Hold);
                }
            }

            return result;
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

