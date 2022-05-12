using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MachinaTrader.Globals.Structure.Interfaces;

namespace MachinaTrader.Strategies
{
    public static class StrategyFactory
    {
        public static List<ITradingStrategy> GetTradingStrategies()
        {
            // Use reflection to get all the instances of our strategies.
            var strategyTypes = Assembly.GetAssembly(typeof(BaseStrategy)).GetTypes()
                                     .Where(type => type.IsSubclassOf(typeof(BaseStrategy)))
                                     .ToList();

            var strategies = new List<ITradingStrategy>();

            foreach (var item in strategyTypes)
            {
                strategies.Add((ITradingStrategy)Activator.CreateInstance(item));
            }

            return strategies;
        }

        public static ITradingStrategy GetTradingStrategy(string strategy)
        {
            var parts = strategy.Split(':');
            // Use reflection to get all the instances of our strategies.
            var strategyTypes = Assembly.GetAssembly(typeof(BaseStrategy)).GetTypes()
                                     .Where(type => type.IsSubclassOf(typeof(BaseStrategy)))
                                     .ToList();

            foreach (var item in strategyTypes)
            {
                var s = (ITradingStrategy)Activator.CreateInstance(item);
                if (s.Name.ToLowerInvariant() == parts[0].ToLowerInvariant())
                {
                    if (parts.Length > 1) s.Parameters = parts[1];

                    return s;
                }
            }

            
            return null;
        }
    }
}
