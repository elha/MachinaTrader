using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MachinaTrader.Globals.Helpers;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Extensions;
using System.Linq;
using MachinaTrader.Globals;

namespace MachinaTrader.Backtester
{
    public class BackTestRunner
    {
        public async Task<List<BackTestResult>> RunSingleStrategy(ITradingStrategy strategy, BacktestOptions backtestOptions, Dictionary<string, List<Candle>> candleStore, string baseCurrency, bool saveSignals, decimal startingWallet, decimal tradeAmount)
        {
            var results = new List<BackTestResult>();
            var allSignals = new List<TradeSignal>();

            // Go through our coinpairs and backtest them.
            foreach (string globalSymbol in backtestOptions.Coins)
            {
            
                backtestOptions.Coin = globalSymbol;

                var backTestResult = new BackTestResult { Market = globalSymbol };

                try
                {
                    var candles = candleStore[globalSymbol];
                    var trend = strategy.Prepare(candles);
                    var signals = new List<TradeSignal>();

                    for (int i = 0; i < trend.Count; i++)
                    {
                        if (trend[i].Advice == TradeAdviceEnum.Buy)
                        {
                            var id = Guid.NewGuid();

                            signals.Add(new TradeSignal
                            {
                                Id = id,
                                MarketName = globalSymbol,
                                Price = candles[i].Close,
                                TradeAdvice = trend[i],
                                SignalCandle = candles[i],
                                Timestamp = candles[i].Timestamp,
                                StrategyName = strategy.Name
                            });


                            // find next Sell
                            for (int j = i; j < trend.Count; j++)
                            {
                                if (trend[j].Advice == TradeAdviceEnum.Sell)
                                {
                                    var feePercentTwoTrades = 0.0018m * 2m;
                                    var feeTotalTwoTrades = feePercentTwoTrades * tradeAmount;
                                    var currentProfitPercentage = (((candles[j].Close - candles[i].Close) / candles[i].Close) - feePercentTwoTrades) * 100;
                                    var quantity = tradeAmount / candles[i].Close;
                                    var currentProfit = (candles[j].Close - candles[i].Close) * quantity - feeTotalTwoTrades;

                                    backTestResult.Trades.Add(new BackTestTradeResult
                                    {
                                        Market = globalSymbol,
                                        Quantity = quantity,
                                        OpenRate = candles[i].Close,
                                        CloseRate = candles[j].Close,
                                        ProfitPercentage = currentProfitPercentage,
                                        Profit = currentProfit,
                                        Duration = j - i,
                                        StartDate = candles[i].Timestamp,
                                        EndDate = candles[j].Timestamp
                                    });

                                    signals.Add(new TradeSignal
                                    {
                                        Id = Guid.NewGuid(),
                                        ParentId = id,
                                        MarketName = globalSymbol,
                                        Price = candles[j].Close,
                                        TradeAdvice = trend[j],
                                        SignalCandle = candles[j],
                                        Profit = currentProfit,
                                        PercentageProfit = currentProfitPercentage,
                                        Timestamp = candles[j].Timestamp,
                                        StrategyName = strategy.Name
                                    });

                                    if (backtestOptions.OnlyStartNewTradesWhenSold)
                                        i = j;

                                    break;
                                }
                            }
                        }
                    }

                    if (saveSignals)
                    {
                        var candleProvider = new DatabaseCandleProvider();
                        await candleProvider.SaveTradeSignals(backtestOptions, Global.DataStoreBacktest, signals);
                    }

                    allSignals.AddRange(signals);
                }
                catch (Exception ex)
                {
                    ConsoleUtility.WriteColoredLine($"Error in Strategy: {strategy.Name}", ConsoleColor.Red);
                    ConsoleUtility.WriteColoredLine($"\t{ex.Message}", ConsoleColor.Red);
                }

                results.Add(backTestResult);
            }

            allSignals = allSignals.OrderBy(t => t != null).ThenBy(t => t.Timestamp).ToList();

            #region wallet trend

            var strategyTrades = new List<BackTestTradeResult>();
            foreach (var marketResult in results)
            {
                strategyTrades.AddRange(marketResult.Trades);
            }
            strategyTrades = strategyTrades.OrderBy(t => t.StartDate).ToList();           

            decimal wallet = startingWallet;
            decimal lowWallet = startingWallet;

            int cct = 0;
            int mct = 0;

            for (int i = 0; i < allSignals.Count(); i++)
            {
                var signal = allSignals[i];
    
                if (signal.TradeAdvice.Advice == TradeAdviceEnum.Buy)
                {
                    cct = cct + 1;

                    if (cct > mct)
                        mct = cct;

                    wallet = wallet - tradeAmount;
                }
                else if (signal.TradeAdvice.Advice == TradeAdviceEnum.Sell)
                {
                    cct = cct - 1;
                    
                    // reinvest everything
                    tradeAmount += signal.PercentageProfit * tradeAmount * 0.01m;
                    wallet = wallet + tradeAmount;
                    
                    if (wallet < lowWallet)
                        lowWallet = wallet;
                }
            }

            var ff = results.FirstOrDefault();
            if (ff != null)
            {
                results.FirstOrDefault().ConcurrentTrades = mct;
                results.FirstOrDefault().Wallet = wallet;
                results.FirstOrDefault().LowWallet = lowWallet;
            }
            
            #endregion

            return results;
        }

        private SellType ShouldSell(double tradeOpenRate, double currentRateBid, DateTime utcNow)
        {
            //var currentProfit = (currentRateBid - tradeOpenRate) / tradeOpenRate;

            //if (currentProfit < -0.07) //stopLossPercentage
            //    return SellType.StopLoss;

            //if (currentProfit >= 0.08)
            //    return SellType.Immediate;

            //if (currentProfit > 0.04)
            //	return SellType.Timed;

            // Check if time matches and current rate is above threshold
            //foreach (var item in returnOnInvestment)
            //{
            //	var timeDiff = (utcNow - tradeOpenRate).TotalSeconds / 60;

            //	if (timeDiff >= item.Duration && currentProfit > item.Profit)
            //		return SellType.Timed;
            //}

            return SellType.None;
        }
    }
}
