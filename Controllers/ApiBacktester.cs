using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExchangeSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mynt.Core.Backtester;
using Mynt.Core.Enums;
using Mynt.Core.Exchanges;
using Mynt.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyntUI.Controllers
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

    [Authorize, Route("api/trading/candlesAge")]
    public class ApiTradingBacktesterCandlesAge : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string candleSize = "5")
        {

            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
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
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string candleSize = "5")
        {
            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
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
        public async Task<ActionResult> Get(string exchange)
        {
            JArray symbolArray = new JArray();
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange.ToLower());
            var exchangeCoins = await api.GetSymbolsAsync();
            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(api.ExchangeSymbolToGlobalSymbol(coin));

            }
            return new JsonResult(symbolArray);
        }
    }

    [Authorize, Route("api/trading/backtester")]
    public class ApiBacktesterDisplay : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Get(string exchange, string coinsToBuy, string candleSize = "5", string strategy = "all")
        {
            JObject strategies = new JObject();

            List<string> coins = new List<string>();
            Char delimiter = ',';
            String[] coinsToBuyArray = coinsToBuy.Split(delimiter);
            foreach (var coin in coinsToBuyArray)
            {
                coins.Add(coin.ToUpper());
            }

            BacktestOptions backtestOptions = new BacktestOptions();
            backtestOptions.Exchange = (Exchange)Enum.Parse(typeof(Exchange), exchange, true);
            backtestOptions.Coins = coins;
            backtestOptions.CandlePeriod = Int32.Parse(candleSize);


            foreach (var tradingStrategy in BacktestFunctions.GetTradingStrategies())
            {
                if (strategy != "all")
                {
                    var base64EncodedBytes = Convert.FromBase64String(strategy);
                    if (tradingStrategy.Name != Encoding.UTF8.GetString(base64EncodedBytes))
                    {
                        continue;
                    }
                }
                var result = await BacktestFunctions.BackTestJson(tradingStrategy, backtestOptions, Globals.GlobalDataStoreBacktest);
                await Globals.GlobalHubMyntBacktest.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(result[0]));
            }
            return new JsonResult(strategies);
        }
    }

}
