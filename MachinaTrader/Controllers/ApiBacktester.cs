using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using ExchangeSharp;
using MachinaTrader.TradeManagers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mynt.Core.Backtester;
using Mynt.Core.Enums;
using Mynt.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{
    [Route("api/backtest/refresh")]
    public class BacktestRefresh : Controller
    {
        [HttpGet]
        public async Task<string> Get(string exchange, string coinsToBuy, string candleSize = "5")
        {
            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange) Enum.Parse(typeof(Exchange), exchange, true),
                Coins = new List<string>(new[] {coinsToBuy}),
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Runtime.GlobalDataStoreBacktest);

            return "Refresh Done";
        }
    }

    /// <summary>
    /// Get the candle ages of selected coins
    /// Return value JObject
    /// </summary>
    [Authorize, Route("api/trading/candlesAge")]
    public class ApiTradingBacktesterCandlesAge : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
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
                Exchange = (Exchange) Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            JObject result = new JObject
            {
                ["result"] = await DataRefresher.GetCacheAge(backtestOptions, Runtime.GlobalDataStoreBacktest)
            };
            return new JsonResult(result);
        }
    }

    [Authorize, Route("api/trading/refreshCandles")]
    public class ApiTradingBacktesterRefreshCandles : Controller
    {

        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5")
        {
            List<string> coins = new List<string>();

            if (String.IsNullOrEmpty(coinsToBuy))
            {
                IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
                var exchangeCoins = api.GetSymbolsMetadataAsync().Result.Where(m=>m.BaseCurrency == baseCurrency);
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
                Exchange = (Exchange) Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };

            await DataRefresher.RefreshCandleData(x => Global.Logger.Information(x), backtestOptions, Runtime.GlobalDataStoreBacktest);
            JObject result = new JObject
            {
                ["result"] = "success"
            };
            return new JsonResult(result);
        }
    }

    [Authorize, Route("api/trading/backtesterStrategy")]
    public class ApiTradingBacktesterStrategy : Controller
    {
        [HttpGet]
        public ActionResult Get()
        {

            JObject strategies = new JObject();
            foreach (var strategy in BacktestFunctions.GetTradingStrategies())
            {
                strategies[strategy.Name] = new JObject
                {
                    ["Name"] = strategy.Name,
                    ["ClassName"] = strategy.ToString().Replace("Mynt.Core.Strategies.", ""),
                    ["IdealPeriod"] = strategy.IdealPeriod.ToString(),
                    ["MinimumAmountOfCandles"] = strategy.MinimumAmountOfCandles.ToString()
                };
            }
            return new JsonResult(strategies);
        }
    }

    [Authorize, Route("api/trading/getExchangePairs")]
    public class ApiTradingGetExchangePairs : Controller
    {
        [HttpGet]
        public ActionResult Get(string exchange, string baseCurrency)
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
            var exchangeBaseCurrencies = api.GetSymbolsMetadataAsync().Result.Select(m=>m.BaseCurrency).Distinct();
            foreach (var currency in exchangeBaseCurrencies)
            {
                baseCurrencyArray.Add(currency);
            }

            result.Add(symbolArray);
            result.Add(baseCurrencyArray);

            return new JsonResult(result);
        }
    }

    [Authorize, Route("api/trading/backtester")]
    public class ApiBacktesterDisplay : Controller
    {
        [HttpGet]
        public ActionResult Get(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5", string strategy = "all")
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

            BacktestOptions backtestOptions = new BacktestOptions
            {
                DataFolder = Global.DataPath,
                Exchange = (Exchange) Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                CandlePeriod = Int32.Parse(candleSize)
            };
            //backtestOptions.StakeAmount = 150;

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
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, Runtime.GlobalDataStoreBacktest);
                foreach (var item in result)
                {
                    await Runtime.GlobalHubMyntBacktest.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(item));
                }
            });

            return new JsonResult(strategies);
        }
    }


    [Route("api/trading/backtest/gettickers")]
    public class ApiTradingBacktestGettickers : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string strategy, string candleSize)
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
            var items = await candleProvider.GetCandles(backtestOptions, Runtime.GlobalDataStoreBacktest);

            return new JsonResult(items);
        }
    }

    [Route("api/trading/backtest/getsignals")]
    public class ApiTradingBacktestGetsignals : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string strategy, string candleSize = "5")
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
                Exchange = (Exchange) Enum.Parse(typeof(Exchange), exchange, true),
                Coins = coins,
                Coin = coinsToBuy,
                CandlePeriod = Int32.Parse(candleSize)
            };

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetSignals(backtestOptions, Runtime.GlobalDataStoreBacktest, strategyName);

            return new JsonResult(items);
        }
    }

    [Route("api/trading/backtest/simulation")]
    public class ApiTradingBacktestSimulation : Controller
    {
        //string exchange, string coinsToBuy, string strategy, string candleSize, string date
        [HttpGet]
        public async Task<bool> Get(string coinsToBuy, string date)
        {
            var currenDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(date, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));

            var backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = Exchange.Gdax;
            backtestOptions.Coin = coinsToBuy;
            backtestOptions.CandlePeriod = Int32.Parse(Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCandleSize);
            Candle databaseFirstCandle = await Runtime.GlobalDataStoreBacktest.GetBacktestFirstCandle(backtestOptions);
            Candle databaseLastCandle = await Runtime.GlobalDataStoreBacktest.GetBacktestLastCandle(backtestOptions);

            var tradeManager = new TradeManager();

            Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate = currenDate > databaseFirstCandle.Timestamp ? currenDate : databaseFirstCandle.Timestamp;
            while (Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate <= databaseLastCandle.Timestamp)
            {
                await tradeManager.LookForNewTrades();
                await tradeManager.UpdateExistingTrades();

                Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate = Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate.AddMinutes(10);

                Console.WriteLine(Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate);
            }

            return true;
        }
    }

}
