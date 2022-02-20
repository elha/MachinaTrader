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

        public static async Task<List<BackTestResult>> BackTest(ITradingStrategy strategy, BacktestOptions backtestOptions, Dictionary<string, List<Candle>> candles, string baseCurrency, bool saveSignals, decimal startingWallet, decimal tradeAmount)
        {
            var runner = new BackTestRunner();
            var results = await runner.RunSingleStrategy(strategy, backtestOptions, candles, baseCurrency, saveSignals, startingWallet, tradeAmount);
            return results;
        }

        public static async Task<JArray> BackTestJson(ITradingStrategy strategy, BacktestOptions backtestOptions, Dictionary<string, List<Candle>> candles, string baseCurrency, bool saveSignals, decimal startingWallet, decimal tradeAmount)
        {
            var results = await BackTest(strategy, backtestOptions, candles, baseCurrency, saveSignals, startingWallet, tradeAmount);

            var jArrayResult = new JArray();

            if (results.Count > 0)
            {
                var resultsSummary = new BackTestStrategyResult();
                resultsSummary.Results = results;
                resultsSummary.Strategy = strategy.Name;
                if(!string.IsNullOrEmpty(strategy.Parameters)) resultsSummary.Strategy += ":" + strategy.Parameters;

                resultsSummary.ConcurrentTrades = results.First().ConcurrentTrades;
                resultsSummary.Wallet = results.First().Wallet;
                resultsSummary.LowWallet = results.First().LowWallet;

                var endWallet = Math.Round(resultsSummary.Wallet, 3);
                var walletRealPercentage = Math.Round(((resultsSummary.Wallet - startingWallet) / startingWallet) * 100, 3);
                var lowWallet = Math.Round(resultsSummary.LowWallet, 3);

                var currentResult1 = new JObject();
                currentResult1["Strategy"] = resultsSummary.Strategy;
                currentResult1["ConcurrentTrades"] = resultsSummary.ConcurrentTrades;
                currentResult1["Wallet"] = endWallet + " " + baseCurrency + " (" + walletRealPercentage + "%)";
                currentResult1["LowWallet"] = lowWallet;
                currentResult1["AmountOfTrades"] = resultsSummary.AmountOfTrades;
                currentResult1["AmountOfProfitableTrades"] = resultsSummary.AmountOfProfitableTrades;
                currentResult1["SuccessRate"] = resultsSummary.SuccessRate;
                currentResult1["TotalProfit"] = resultsSummary.TotalProfit;
                currentResult1["TotalProfitPercentage"] = resultsSummary.TotalProfitPercentage;
                currentResult1["AverageDuration"] = resultsSummary.AverageDuration;
                currentResult1["DataPeriod"] = resultsSummary.DataPeriod;
                currentResult1["BaseCurrency"] = baseCurrency;
                currentResult1["StartDate"] = backtestOptions.StartDate;
                currentResult1["EndDate"] = backtestOptions.EndDate;
                jArrayResult.Add(currentResult1);

                foreach (var result in results)
                {
                    var currentResult = new JObject();
                    currentResult["Market"] = result.Market;
                    currentResult["Strategy"] = resultsSummary.Strategy;
                    currentResult["AmountOfTrades"] = result.AmountOfTrades;
                    currentResult["AmountOfProfitableTrades"] = result.AmountOfProfitableTrades;
                    currentResult["SuccessRate"] = result.SuccessRate;
                    currentResult["TotalProfit"] = result.TotalProfit;
                    currentResult["TotalProfitPercentage"] = result.TotalProfitPercentage;
                    currentResult["AverageDuration"] = result.AverageDuration;
                    currentResult["DataPeriod"] = result.DataPeriod;
                    currentResult["StartDate"] = backtestOptions.StartDate;
                    currentResult["EndDate"] = backtestOptions.EndDate;

                    jArrayResult.Add(currentResult);
                }
            }

            return jArrayResult;
        }
    }
}
