using MachinaTrader.Globals.Structure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Models;
using Newtonsoft.Json.Linq;
using MachinaTrader.Strategies;

namespace MachinaTrader.Backtester
{
    public class BacktestFunctions
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

        public static async Task<List<BackTestResult>> BackTest(ITradingStrategy strategy, BacktestOptions backtestOptions, IDataStoreBacktest dataStore, bool saveSignals, decimal startingWallet, decimal tradeAmount)
        {
            var runner = new BackTestRunner();
            var results = await runner.RunSingleStrategy(strategy, backtestOptions, dataStore, saveSignals, startingWallet, tradeAmount);
            return results;
        }

        public static async Task<JArray> BackTestJson(ITradingStrategy strategy, BacktestOptions backtestOptions, IDataStoreBacktest dataStore, bool saveSignals, decimal startingWallet, decimal tradeAmount)
        {
            List<BackTestResult> results = await BackTest(strategy, backtestOptions, dataStore, saveSignals, startingWallet, tradeAmount);

            var jArrayResult = new JArray();

            if (results.Count > 0)
            {
                var resultsSummary = new BackTestStrategyResult();
                resultsSummary.Results = results;
                resultsSummary.Strategy = strategy.Name;
                resultsSummary.ConcurrentTrades = results.First().ConcurrentTrades;
                resultsSummary.Wallet = results.First().Wallet;

                var currentResult1 = new JObject();
                currentResult1["Strategy"] = resultsSummary.Strategy;
                currentResult1["ConcurrentTrades"] = resultsSummary.ConcurrentTrades;
                currentResult1["Wallet"] = resultsSummary.Wallet;
                currentResult1["AmountOfTrades"] = resultsSummary.AmountOfTrades;
                currentResult1["AmountOfProfitableTrades"] = resultsSummary.AmountOfProfitableTrades;
                currentResult1["SuccessRate"] = resultsSummary.SuccessRate;
                currentResult1["TotalProfit"] = resultsSummary.TotalProfit;
                currentResult1["TotalProfitPercentage"] = resultsSummary.TotalProfitPercentage;
                currentResult1["AverageDuration"] = resultsSummary.AverageDuration;
                currentResult1["DataPeriod"] = resultsSummary.DataPeriod;
                jArrayResult.Add(currentResult1);

                foreach (var result in results)
                {
                    var currentResult = new JObject();
                    currentResult["Market"] = result.Market;
                    currentResult["Strategy"] = strategy.Name;
                    currentResult["AmountOfTrades"] = result.AmountOfTrades;
                    currentResult["AmountOfProfitableTrades"] = result.AmountOfProfitableTrades;
                    currentResult["SuccessRate"] = result.SuccessRate;
                    currentResult["TotalProfit"] = result.TotalProfit;
                    currentResult["TotalProfitPercentage"] = result.TotalProfitPercentage;
                    currentResult["AverageDuration"] = result.AverageDuration;
                    currentResult["DataPeriod"] = result.DataPeriod;

                    jArrayResult.Add(currentResult);
                }
            }

            return jArrayResult;
        }
    }
}
