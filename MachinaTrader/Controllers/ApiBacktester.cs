using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeSharp;
using LazyCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mynt.Core.Backtester;
using Mynt.Core.Enums;
using Mynt.Core.Models;
using MachinaTrader.TradeManagers;
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
            BacktestOptions backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = new List<string>(new[] { coinsToBuy });
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);

            await DataRefresher.RefreshCandleData(x => Console.WriteLine(x), backtestOptions, Globals.GlobalDataStoreBacktest);

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

            BacktestOptions backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);
            JObject result = new JObject();
            result["result"] = await DataRefresher.GetCacheAge(backtestOptions, Globals.GlobalDataStoreBacktest);
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

            BacktestOptions backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);

            await DataRefresher.RefreshCandleData(x => Console.WriteLine(x), backtestOptions, Globals.GlobalDataStoreBacktest);
            JObject result = new JObject();
            result["result"] = "success";
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
                strategies[strategy.Name] = new JObject();
                strategies[strategy.Name]["Name"] = strategy.Name;
                strategies[strategy.Name]["ClassName"] = strategy.ToString().Replace("Mynt.Core.Strategies.", "");
                strategies[strategy.Name]["IdealPeriod"] = strategy.IdealPeriod.ToString();
                strategies[strategy.Name]["MinimumAmountOfCandles"] = strategy.MinimumAmountOfCandles.ToString();
            }
            return new JsonResult(strategies);
        }
    }

    [Authorize, Route("api/trading/getExchangePairs")]
    public class ApiTradingGetExchangePairs : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string baseCurrency)
        {
            var result = new JArray();

            var symbolArray = new JArray();

            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            var exchangeCoins = api.GetSymbolsMetadataAsync().Result;

            if (!String.IsNullOrEmpty(baseCurrency))
                exchangeCoins = exchangeCoins.Where(e => e.BaseCurrency.ToLowerInvariant() == baseCurrency.ToLowerInvariant());

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
    }

    [Authorize, Route("api/trading/backtester")]
    public class ApiBacktesterDisplay : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string baseCurrency, string candleSize = "5", string strategy = "all")
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

            BacktestOptions backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);
            //backtestOptions.StakeAmount = 150;

            var cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = cts.Token;
            parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
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
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, Globals.GlobalDataStoreBacktest);
                foreach (var item in result)
                {
                    await Globals.GlobalHubMyntBacktest.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(item));
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
            var strategyName = WebUtility.HtmlDecode(strategy);

            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            var backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.Coin = coinsToBuy;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetCandles(backtestOptions, Globals.GlobalDataStoreBacktest);

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

            var backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.Coin = coinsToBuy;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetSignals(backtestOptions, Globals.GlobalDataStoreBacktest, strategyName);

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
            backtestOptions.CandlePeriod = Int32.Parse(Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCandleSize);
            Candle databaseFirstCandle = await Globals.GlobalDataStoreBacktest.GetBacktestFirstCandle(backtestOptions);
            Candle databaseLastCandle = await Globals.GlobalDataStoreBacktest.GetBacktestLastCandle(backtestOptions);

            var tradeManager = new TradeManager();

            Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate = currenDate>databaseFirstCandle.Timestamp? currenDate: databaseFirstCandle.Timestamp;
            while (Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate <= databaseLastCandle.Timestamp)
            {
                await tradeManager.LookForNewTrades();
                await tradeManager.UpdateExistingTrades();

                Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate = Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate.AddMinutes(10);

                Console.WriteLine(Globals.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate);
            }        

            return true;
        }
    }
}
