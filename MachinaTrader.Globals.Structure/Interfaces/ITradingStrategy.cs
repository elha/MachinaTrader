using System;
using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using Serilog;

namespace MachinaTrader.Globals.Structure.Interfaces
{
    public interface ITradingStrategy
    {
        string Name { get; set;  }

        int MinimumAmountOfCandles { get; }

        /// <summary>
        /// parameters each from 00 to 99
        /// </summary>
        string Parameters { get; set; }
        string MinParameters { get; set; }
        string MaxParameters { get; set; }

        Period IdealPeriod { get; }
 

        /// <summary>
        /// Gets a list of trade advices, one for each of the candles provided as input.
        /// </summary>
        /// <param name="candles">
        /// The list of candles to based the trade advices on.
        /// </param>
        /// <returns>
        /// A list of trade advices. The length of the list matches the length of the input list.
        /// </returns>
        List<TradeAdvice> Prepare(List<Candle> candles);
    }
}
