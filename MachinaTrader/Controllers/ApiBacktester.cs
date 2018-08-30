using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using ExchangeSharp;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.TradeManagers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MachinaTrader.Backtester;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Exchanges;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/backtester/")]
    public class ApiBacktester : Controller
    {
        [HttpGet]
        [Route("refresh")]
        public async Task<string> Refresh(string exchange, string coinsToBuy, string candleSize = "5")
        {
            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = new List<string>(new[] { coinsToBuy }),
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Global.DataStoreBacktest);

            return "Refresh Done";
        }

        [HttpGet]
        [Route("candlesAge")]
        public async Task<ActionResult> CandlesAge(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            JObject result = new JObject
            {
                ["result"] = await DataRefresher.GetCacheAge(backtestOptions, Global.DataStoreBacktest)
            };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("refreshCandles")]
        public async Task<ActionResult> RefreshCandles(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Global.DataStoreBacktest);
            JObject result = new JObject
            {
                ["result"] = "success"
            };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterStrategy")]
        public ActionResult BacktesterStrategy()
        {

            JObject strategies = new JObject();
            foreach (var strategy in BacktestFunctions.GetTradingStrategies())
            {
                strategies[strategy.Name] = new JObject
                {
                    ["Name"] = strategy.Name,
                    ["ClassName"] = strategy.ToString().Replace("MachinaTrader.Strategies.", ""),
                    ["IdealPeriod"] = strategy.IdealPeriod.ToString(),
                    ["MinimumAmountOfCandles"] = strategy.MinimumAmountOfCandles.ToString()
                };
            }
            return new JsonResult(strategies);
        }

        [HttpGet]
        [Route("exchangePairs")]
        public ActionResult ExchangePairs(string exchange, string baseCurrency)
        {
            var result = new JArray();

            var symbolArray = new JArray();

            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            var exchangeCoins = api.GetSymbolsMetadataAsync().Result;

            if (!String.IsNullOrEmpty(baseCurrency))
            {
                exchangeCoins = exchangeCoins.Where(e => e.BaseCurrency.ToLowerInvariant() == baseCurrency.ToLowerInvariant());
            }

            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
            }

            var baseCurrencyArray = new JArray();
            var exchangeBaseCurrencies = api.GetSymbolsMetadataAsync().Result.Select(m => m.BaseCurrency).Distinct();
            foreach (var currency in exchangeBaseCurrencies)
            {
                baseCurrencyArray.Add(currency);
            }

            result.Add(symbolArray);
            result.Add(baseCurrencyArray);

            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterResults")]
        public ActionResult BacktesterResults(string exchange, string coinsToBuy, string baseCurrency, bool saveSignals, decimal startingWallet, decimal tradeAmount, DateTime? from = null, DateTime? to = null, string candleSize = "5", string strategy = "all")
        {
            JObject strategies = new JObject();

            List<string> coins = new List<string>();
            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(api.ExchangeSymbolToGlobalSymbol(coin.MarketName));
                }
            }
            else
            {
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(coin.ToUpper());
                }
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            if (from.HasValue)
                backtestOptions.StartDate = from.Value;

            if (to.HasValue)
                backtestOptions.EndDate = to.Value;

            var cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            Parallel.ForEach(BacktestFunctions.GetTradingStrategies(), parallelOptions, async tradingStrategy =>
            {
                if (strategy != "all")
                {
                    var base64EncodedBytes = Convert.FromBase64String(strategy);
                    if (tradingStrategy.Name != Encoding.UTF8.GetString(base64EncodedBytes))
                    {
                        return;
                    }
                }
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, Global.DataStoreBacktest, saveSignals, startingWallet, tradeAmount);
                for (int i = 0; i < result.Count(); i++)
                {
                    if (i == 0)
                    {
                        await Runtime.GlobalHubBacktest.Clients.All.SendAsync("SendSummary", JsonConvert.SerializeObject(result[i]));
                    }
                    else
                    {
                        await Runtime.GlobalHubBacktest.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(result[i]));
                    }
                }
            });

            return new JsonResult(strategies);
        }


        [HttpGet]
        [Route("getTickers")]
        public async Task<ActionResult> GetTickers(string exchange, string coinsToBuy, string strategy, string candleSize)
        {
            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                Coin = coinsToBuy,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);

            return new JsonResult(items);
        }

        [HttpGet]
        [Route("getSignals")]
        public async Task<ActionResult> GetSignals(string exchange, string coinsToBuy, string strategy, string candleSize = "5")
        {
            var strategyName = WebUtility.HtmlDecode(strategy);

            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            var backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                Coin = coinsToBuy,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetSignals(backtestOptions, Global.DataStoreBacktest, strategyName);

            return new JsonResult(items);
        }

        [HttpGet]
        [Route("simulation")]
        public async Task<bool> Simulation(string exchange,
                                           string coinsToBuy,
                                           string baseCurrency,
                                            bool saveSignals,
                                           decimal startingWallet,
                                           decimal tradeAmount,
                                           DateTime? from,
                                           DateTime? to,
                                           string candleSize,
                                           string strategy)
        {
#warning //TODO: clone static objects configurations to restore after simulation

            var exchangeEnum = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);

            var currentExchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();
            currentExchangeOption.Exchange = exchangeEnum;
            currentExchangeOption.IsSimulation = true;
            currentExchangeOption.SimulationCandleSize = candleSize;
            if (startingWallet != 0m)
                currentExchangeOption.SimulationStartingWallet = startingWallet;

            Global.Configuration.TradeOptions.QuoteCurrency = baseCurrency;
            Global.Configuration.TradeOptions.PaperTrade = false;
            if (tradeAmount != 0m)
                Global.Configuration.TradeOptions.AmountToInvestPerTrader = tradeAmount;

            switch (exchangeEnum)
            {
                case Exchange.Gdax:
                    Global.ExchangeApi = new BaseExchange(currentExchangeOption, new ExchangeSimulationApi(new ExchangeGdaxAPI()));
                    break;

                case Exchange.Binance:
                    Global.ExchangeApi = new BaseExchange(currentExchangeOption, new ExchangeSimulationApi(new ExchangeBinanceAPI()));
                    break;
            }

            Global.DataStore = new MemoryDataStore();

            var candleProvider = new DatabaseCandleProvider();
            var globalFullApi = await Global.ExchangeApi.GetFullApi();
            if (!String.IsNullOrEmpty(coinsToBuy))
            {
                var coins = new List<string>();
                Char delimiter = ',';
                String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
                foreach (var coin in coinsToBuyArray)
                {
                    coins.Add(globalFullApi.GlobalSymbolToExchangeSymbol(coin.ToUpper()));
                }

                Global.Configuration.TradeOptions.OnlyTradeList = coins;
            }

            var firstAndLastCandleDates = await candleProvider.CacheAllData(globalFullApi, Global.Configuration.ExchangeOptions.FirstOrDefault().Exchange);

            var tradeManager = new TradeManager();

            //var simulationStartingDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(from, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            //var simulationEndingDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(to, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            var simulationStartingDate = firstAndLastCandleDates.Item1;
            if (from.HasValue)
                simulationStartingDate = TimeZoneInfo.ConvertTimeToUtc(from.Value);

            var simulationEndingDate = firstAndLastCandleDates.Item2;
            if (to.HasValue)
            {
                simulationEndingDate = TimeZoneInfo.ConvertTimeToUtc(to.Value);
                simulationEndingDate = simulationEndingDate.AddDays(1).AddMinutes(-1);
            }

            currentExchangeOption.SimulationCurrentDate = simulationStartingDate;
            currentExchangeOption.SimulationEndDate = simulationEndingDate;

            var base64EncodedBytes = Convert.FromBase64String(strategy);
            var strategyName = Encoding.UTF8.GetString(base64EncodedBytes);
            var strategyClassName = string.Empty;
            foreach (var item in BacktestFunctions.GetTradingStrategies())
            {
                if (item.Name != strategyName)
                {
                    continue;
                }
                strategyClassName = item.ToString().Split('.').Last();
            }
           

            while (currentExchangeOption.SimulationCurrentDate <= currentExchangeOption.SimulationEndDate)
            {
                Global.Logger.Information($"------ SimulationCurrentDate start: {currentExchangeOption.SimulationCurrentDate}");
                var watch1 = System.Diagnostics.Stopwatch.StartNew();

                await tradeManager.LookForNewTrades(strategyClassName);
                await tradeManager.UpdateExistingTrades();

                currentExchangeOption.SimulationCurrentDate = currentExchangeOption.SimulationCurrentDate.AddMinutes(5);

                watch1.Stop();
                Global.Logger.Information($"------SimulationCurrentDate end: {currentExchangeOption.SimulationCurrentDate} in #{watch1.Elapsed.TotalSeconds} seconds");

                await Runtime.GlobalHubBacktest.Clients.All.SendAsync("SendSimulationStatus", JsonConvert.SerializeObject(currentExchangeOption.SimulationCurrentDate));
            }

            return true;
        }

        [Route("stopSimulation")]
        public async Task<bool> StopSimulation()
        {
            var currentExchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();
            currentExchangeOption.SimulationEndDate = DateTime.MinValue;

            return true;
        }
    }
}
