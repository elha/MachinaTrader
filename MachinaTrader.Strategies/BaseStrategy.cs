using System;
using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using Serilog;

namespace MachinaTrader.Strategies
{
    public abstract class BaseStrategy : ITradingStrategy
    {
        public abstract string Name { get; set;  }
        public abstract int MinimumAmountOfCandles { get; }
        public abstract Period IdealPeriod { get; }
        public virtual string Parameters { get; set; } = "";
        public virtual string MinParameters { get; set; } = "";
        public virtual string MaxParameters { get; set; } = "";

        public abstract List<TradeAdvice> Prepare(List<Candle> candles);
    }
}
