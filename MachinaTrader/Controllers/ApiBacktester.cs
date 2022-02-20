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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MachinaTrader.Backtester;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Strategies;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/backtester/")]
    public class ApiBacktester : Controller
    {
        [HttpGet]
        [Route("refresh")]
        public async Task<string> Refresh(string exchange, string coinsToBuy, string candleSize = "5")
        {
            var backtestOptions = new BacktestOptions
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
            var coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
                var exchangeCoins = api.GetMarketSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == baseCurrency);
                foreach (var coin in exchangeCoins)
                {
                    coins.Add(await api.ExchangeMarketSymbolToGlobalMarketSymbolAsync(coin.MarketSymbol));
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
                CandlePeriod = Int32.Parse(candleSize),
                EndDate = DateTime.Now
            };

            var result = new JObject
            {
                ["result"] = await DataRefresher.GetCacheAge(backtestOptions, Global.DataStoreBacktest)
            };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("refreshCandles")]
        public async Task<ActionResult> RefreshCandles(string exchange, string coinsToBuy, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (!String.IsNullOrEmpty(coinsToBuy))
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
        [Route("fillCandlesGaps")]
        public async Task<ActionResult> FillCandlesGaps(string exchange, string coinsToBuy, DateTime? from = null, DateTime? to = null, string candleSize = "5")
        {
            var coins = new List<string>();

            if (!String.IsNullOrEmpty(coinsToBuy))
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
                CandlePeriod = Int32.Parse(candleSize),
                EndDate = DateTime.Now
            };

            if (from.HasValue)
                backtestOptions.StartDate = from.Value;

            if (to.HasValue)
            {
                backtestOptions.EndDate = to.Value;
                backtestOptions.EndDate = backtestOptions.EndDate.AddDays(1).AddMinutes(-1);
            }

            await DataRefresher.FillCandlesGaps(x => Global.Logger.Information(x), backtestOptions, Global.DataStoreBacktest);

            var result = new JObject { ["result"] = "success" };
            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterStrategy")]
        public ActionResult BacktesterStrategy()
        {

            JObject strategies = new JObject();
            foreach (var strategy in StrategyFactory.GetTradingStrategies())
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
        public ActionResult ExchangePairs(string exchange, string quoteCurrency)
        {
            var result = new JArray();

            var symbolArray = new JArray();

            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
            var exchangeCoins = api.GetMarketSymbolsMetadataAsync().Result;

            if (!String.IsNullOrEmpty(quoteCurrency))
            {
                exchangeCoins = exchangeCoins.Where(e => e.QuoteCurrency.ToLowerInvariant() == quoteCurrency.ToLowerInvariant());
            }

            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(api.ExchangeMarketSymbolToGlobalMarketSymbolAsync(coin.MarketSymbol).Result);
            }

            var quoteCurrencyArray = new JArray();
            var exchangeBaseCurrencies = api.GetMarketSymbolsMetadataAsync().Result.Select(m => m.QuoteCurrency).Distinct();
            foreach (var currency in exchangeBaseCurrencies)
            {
                quoteCurrencyArray.Add(currency);
            }

            result.Add(symbolArray);
            result.Add(quoteCurrencyArray);

            return new JsonResult(result);
        }

        [HttpGet]
        [Route("backtesterResults")]
        public ActionResult BacktesterResults(string exchange, string coinsToBuy, string quoteCurrency, bool saveSignals, decimal startingWallet, decimal tradeAmount, DateTime? from = null, DateTime? to = null, string candleSize = "5", string strategy = "all")
        {
            var strategies = new JObject();
            Runtime.GlobalHubBacktest.Clients.All.SendAsync("Status", $"{{ \"status\": \"running\", \"progress\":\"0 %\"}}");

            var coins = new List<string>();
            if (!String.IsNullOrEmpty(coinsToBuy))
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
            {
                backtestOptions.EndDate = to.Value;
                backtestOptions.EndDate = backtestOptions.EndDate.AddDays(1).AddMinutes(-1);
            }

            if (tradeAmount == 0m)
                tradeAmount = backtestOptions.StakeAmount;

            if (startingWallet == 0m)
                startingWallet = backtestOptions.StartingWallet;

            var cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            var strategyList = StrategyFactory.GetTradingStrategies();

            if (strategy != "all")
            {
                strategy = Encoding.UTF8.GetString(Convert.FromBase64String(strategy));
                var s = strategyList.Find(s => strategy == s.Name);
                strategyList.Clear();
                if(!string.IsNullOrEmpty(s.Parameters))
                {
                    for(int i = int.Parse(s.MinParameters) ; i<= int.Parse(s.MaxParameters); i++)
                    {
                        var n = StrategyFactory.GetTradingStrategy(s.Name);
                        n.Parameters = i.ToString().PadLeft(s.Parameters.Length, '0');
                        var bInvalid = false;
                        for(int j = 0; j < n.Parameters.Length; j++)
                        {
                            if (n.Parameters[j] > n.MaxParameters[j]) bInvalid = true;
                        }
                        if(!bInvalid) strategyList.Add(n);
                    }
                }
                else
                {
                    strategyList.Add(s);
                }
            }

            var progress = 0;

            var candleProvider = new DatabaseCandleProvider();
            var candles = new Dictionary<string, List<Candle>>(); 
            foreach (var coin in coins)
            {
                backtestOptions.Coin = coin;
                candles[coin] = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);
                candles[coin] = candles[coin].FillCandleGaps((Period)Enum.Parse(typeof(Period), backtestOptions.CandlePeriod.ToString(), true)).Result;
            }

            Parallel.ForEach(strategyList, parallelOptions, async tradingStrategy =>
            {
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, candles, quoteCurrency, saveSignals, startingWallet, tradeAmount);
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
                await Runtime.GlobalHubBacktest.Clients.All.SendAsync("Status", $"{{ \"status\": \"running\", \"progress\":\"{progress++ * 100 / strategyList.Count} %\"}}");
            });

            Runtime.GlobalHubBacktest.Clients.All.SendAsync("Status", $"{{ \"status\": \"completed\" }}");

            return new JsonResult(strategies);
        }


        [HttpGet]
        [Route("getTickers")]
        public async Task<ActionResult> GetTickers(string exchange, string coinsToBuy, string strategy, string candleSize, DateTime startDate, DateTime endDate)
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
                CandlePeriod = Int32.Parse(candleSize),
                StartDate = startDate,
                EndDate = endDate
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);

            return new JsonResult(items);
        }

        [HttpGet]
        [Route("getSignals")]
        public async Task<ActionResult> GetSignals(string exchange, string coinsToBuy, string strategy, string candleSize, DateTime startDate, DateTime endDate)
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
                CandlePeriod = Int32.Parse(candleSize),
                StartDate = startDate,
                EndDate = endDate
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetSignals(backtestOptions, Global.DataStoreBacktest, strategyName);

            return new JsonResult(items);
        }

    }
}
