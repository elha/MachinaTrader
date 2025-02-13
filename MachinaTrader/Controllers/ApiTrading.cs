using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MachinaTrader.Models;
using ExchangeSharp;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Strategies;
using MachinaTrader.TradeManagers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{

    [Authorize, Route("api/trading/")]
    public class ApiTrading : Controller
    {
        [HttpGet]
        [Route("exchangePairsExchangeSymbols")]
        public async Task<ActionResult> ExchangePairsExchangeSymbols(string exchange)
        {
            JArray symbolArray = new JArray();
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
            var exchangeCoins = await api.GetMarketSymbolsAsync();
            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(coin);
            }
            return new JsonResult(symbolArray);
        }

        [HttpGet]
        [Route("exchangeCurrencies")]
        public async Task<ActionResult> ExchangeCurrencies(string exchange = "kraken")
        {
            JArray symbolArray = new JArray();
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
            var exchangeCoins = api.GetCurrenciesAsync().Result;
            foreach (var coin in exchangeCoins)
            {
                symbolArray.Add(coin.Key);
            }
            return new JsonResult(symbolArray);
        }

        [HttpGet]
        [Route("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var fullApi = Global.ExchangeApi.GetFullApi();
            var balance = await fullApi.GetAmountsAvailableToTradeAsync();
            return new JsonResult(balance);
        }

        [HttpGet]
        [Route("history")]
        public async Task<IActionResult> GetHistory()
        {
            var fullApi = Global.ExchangeApi.GetFullApi();
            var balance = await fullApi.GetCompletedOrderDetailsAsync("ETHBTC");
            return new JsonResult(balance);
        }

        [HttpGet]
        [Route("getTicker")]
        public async Task<IActionResult> GetTicker(string symbol)
        {
            var ticker = await Global.ExchangeApi.GetTicker(symbol);
            return new JsonResult(ticker);
        }

        [HttpGet]
        [Route("topVolumeCurrencies")]
        public async Task<IActionResult> GetTopVoumeCurrencies(int limit = 20)
        {
            var fullApi = Global.ExchangeApi.GetFullApi();
            var getCurrencies = fullApi.GetTickersAsync().Result;
            var objListOrder = getCurrencies
                .OrderByDescending(o => o.Value.Volume.QuoteCurrencyVolume)
                .ToList();

            JArray topCurrencies = new JArray();
            int count = 0;
            foreach (var currency in objListOrder)
            {
                topCurrencies.Add(currency.Key);
                count = count + 1;
                if (count > limit)
                {
                    break;
                }
            }
            return new JsonResult(topCurrencies);
        }

        [HttpGet]
        [Route("globalToExchangeSymbol")]
        public string GlobalToExchangeSymbol(string exchange, string symbol)
        {
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
            string exchangeSymbol = api.GlobalMarketSymbolToExchangeMarketSymbolAsync(symbol).Result;
            return exchangeSymbol;
        }

        [HttpGet]
        [Route("globalToTradingViewSymbol")]
        public string GlobalToTradingViewSymbol(string exchange, string symbol)
        {
            //Trading view use same format as Binance -> BTC-ETH is ETHBTC
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchange);
            string exchangeSymbol = api.GlobalMarketSymbolToExchangeMarketSymbolAsync(symbol).Result;
            return exchangeSymbol;
        }

        [HttpGet]
        [Route("trade/{tradeId}")]
        public async Task<IActionResult> TradingTrade(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);
            if (trade == null)
            {
                var closedTrades = await Global.DataStore.GetClosedTradesAsync(DateTime.UtcNow.AddHours(-48));
                trade = closedTrades.FirstOrDefault(x => x.TradeId == tradeId);
            }

            return new JsonResult(trade);
        }

        [HttpGet]
        [Route("webSocketValues")]
        public IActionResult GetWebSocketValues()
        {
            return new JsonResult(Runtime.WebSocketTickers);

        }

        [HttpGet]
        [Route("sellNow/{tradeId}")]
        public async Task TradingSellNow(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);

            if (trade == null)
            {
                return;
            }

            var orderId = "";
            if (trade.IsPaperTrading)
                orderId = "PaperTrade-" + Guid.NewGuid().ToString().Replace("-", "");
            else if(trade.PositionType == PositionType.Long)
                orderId = await Global.ExchangeApi.Sell(trade.Market, trade.Quantity, trade.TickerLast.Mid());
            else
                orderId = await Global.ExchangeApi.Buy(trade.Market, trade.Quantity, trade.TickerLast.Mid());

            trade.CloseRate = trade.TickerLast.Mid();
            trade.OpenOrderId = orderId;
            trade.SellOrderId = orderId;
            trade.SellType = SellType.Manually;
            trade.IsSelling = true;

            await Global.DataStore.SaveTradeAsync(trade);

            ////Trigger Sell
            //TradeManager tradeManager = new TradeManager();
            //await tradeManager.UpdateOpenSellOrders(trade);

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to SellNow");
        }

        [HttpGet]
        [Route("cancelOrder/{tradeId}")]
        public async Task TradingCancelOrder(string tradeId)
        {
            var activeTrade = await Global.DataStore.GetActiveTradesAsync();
            var trade = activeTrade.FirstOrDefault(x => x.TradeId == tradeId);

            if (trade == null)
            {
                return;
            }

            if (trade.IsBuying)
            {
                if (!trade.IsPaperTrading)
                    try
                    {
                        await Global.ExchangeApi.CancelOrder(trade.BuyOrderId, trade.Market);
                    }
                    catch { }

                trade.IsBuying = false;
                trade.OpenOrderId = null;
                trade.IsOpen = false;
                trade.SellType = SellType.Cancelled;
                trade.CloseDate = DateTime.UtcNow;
                await Global.DataStore.SaveTradeAsync(trade);
            }

            if (trade.IsSelling)
            {
                //Reenable in active trades
                if (!trade.IsPaperTrading)
                    try
                    {
                        await Global.ExchangeApi.CancelOrder(trade.SellOrderId, trade.Market);
                    } catch { }

                trade.IsSelling = false;
                trade.OpenOrderId = null;
                trade.IsOpen = true;
                trade.SellType = SellType.Cancelled;
                trade.CloseDate = DateTime.UtcNow;
                await Global.DataStore.SaveTradeAsync(trade);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to SellNow");
        }


        [HttpGet]
        [Route("hold/{tradeId}/{holdBoolean}")]
        public async Task TradingHold(string tradeId, bool holdBoolean)
        {
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            var tradeToUpdate = activeTrades.FirstOrDefault(x => x.TradeId == tradeId);
            if (tradeToUpdate != null)
            {
                tradeToUpdate.SellNow = false;
                tradeToUpdate.HoldPosition = holdBoolean;
                await Global.DataStore.SaveTradeAsync(tradeToUpdate);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to Hold");
        }

        [HttpGet]
        [Route("sellOnProfit/{tradeId}/{profitPercentage}")]
        public async Task TradingSellOnProfit(string tradeId, decimal profitPercentage)
        {
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            var tradeToUpdate = activeTrades.FirstOrDefault(x => x.TradeId == tradeId);
            if (tradeToUpdate != null)
            {
                tradeToUpdate.SellNow = false;
                tradeToUpdate.HoldPosition = false;
                tradeToUpdate.SellOnPercentage = profitPercentage;

                await Global.DataStore.SaveTradeAsync(tradeToUpdate);
            }

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", "Set " + tradeId + " to Hold");
        }

        [HttpGet]
        [Route("tradersTester")]
        public IActionResult MyntTradersTester()
        {
            JObject testJson = JObject.Parse(System.IO.File.ReadAllText("wwwroot/views/mynt_traders.json"));
            return new JsonResult(testJson);
        }

        [HttpGet]
        [Route("traders")]
        public async Task<IActionResult> Traders()
        {
            var traders = await Global.DataStore.GetTradersAsync();
            return new JsonResult(traders);
        }

        [HttpGet]
        [Route("activeTradesWithTrader")]
        public async Task<IActionResult> GetActiveTradesWithTrader()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();

            JObject activeTradesJson = new JObject();

            // Get information for active trade
            foreach (var activeTrade in activeTrades)
            {
                activeTradesJson[activeTrade.TraderId] = JObject.FromObject(activeTrade);
            }

            return new JsonResult(activeTradesJson);
        }

        [HttpGet]
        [Route("activeTrades")]
        public async Task<IActionResult> GetActiveTrades()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            return new JsonResult(activeTrades);
        }

        [HttpGet]
        [Route("openTrades")]
        public async Task<IActionResult> GetOpenTrades()
        {
            // Get trades
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();
            return new JsonResult(activeTrades.Where(x => x.IsSelling || x.IsBuying));
        }

        [HttpGet]
        [Route("closedTrades")]
        public async Task<IActionResult> GetClosedTrades(int maxAge = 0)
        {
            // Get trades
            var since = DateTime.UtcNow.AddHours(-maxAge);
            var closedTrades = await Global.DataStore.GetClosedTradesAsync(since);
            return new JsonResult(closedTrades);
        }

        [HttpPost]
        [Route("manualBuy")]
        public async void PostManualBuy([FromBody]JObject data)
        {
            //Debug
            Console.WriteLine(data);

            if ((string)data["manualBuyCurrency"] == "" || (string)data["manualBuyCurrency"] == null)
            {
                return;
            }

            /*
            //var externalTicker = await Global.ExchangeApi.GetTicker("LINKBTC");
            var externalTicker = await Global.ExchangeApi.GetTickerHistory((string)data["manualBuyCurrency"], Period.Minute, DateTime.UtcNow.AddMinutes(-5));

            //Dont care about Ticker for know -> Override Data with manually values
            var lastExternalTicker = externalTicker.LastOrDefault();

            lastExternalTicker.Open = (decimal)data["manualBuyPrice"];
            lastExternalTicker.High = (decimal)data["manualBuyPrice"];
            lastExternalTicker.Low = (decimal)data["manualBuyPrice"];
            lastExternalTicker.Close = (decimal)data["manualBuyPrice"];

            TradeSignal tradeSignal = new TradeSignal();

            string globalExchangeCurrency = await Global.ExchangeApi.ExchangeCurrencyToGlobalCurrency((string)data["manualBuyCurrency"]);
            var globalExchangeCurrencyArray = globalExchangeCurrency.Split("-");

            tradeSignal.MarketName = (string)data["manualBuyCurrency"];
            tradeSignal.QuoteCurrency = globalExchangeCurrencyArray[1];
            tradeSignal.BaseCurrency = globalExchangeCurrencyArray[0];
            tradeSignal.TradeAdvice = TradeAdvice.StrongBuy;
            tradeSignal.SignalCandle = lastExternalTicker;
            */

            //var trade = new Globals.Structure.Models.Trade()
            //{
            //    Market = (string)data["manualBuyCurrency"],
            //    OpenRate = (decimal)data["manualBuyPrice"],
            //    Quantity = (decimal)data["manualBuyAmount"],
            //    StakeAmount = (decimal)data["manualBuyOrderTotal"],
            //};


            //Globals.Structure.Interfaces.ITradeManager tradeManager = new TradeManagerBasket();
            //await tradeManager.CreateTradeOrder(trade);
        }
    }
}
