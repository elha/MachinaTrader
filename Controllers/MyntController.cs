using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Mynt.Core.Interfaces;
using Mynt.Core.TradeManagers;
using MyntUI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyntUI.Controllers
{

    [Route("api/mynt/")]
    public class MyntApiController : Controller
    {
        [HttpGet]
        [Route("trading/TradersTester")]
        public IActionResult MyntTradersTester()
        {
            JObject testJson = JObject.Parse(System.IO.File.ReadAllText("wwwroot/views/mynt_traders.json"));
            return new JsonResult(testJson);
        }

        [HttpGet]
        [Route("trading/Traders")]
        public async Task<IActionResult> Traders()
        {
            var traders = await Globals.GlobalDataStore.GetTradersAsync();
            return new JsonResult(traders);
        }

        [HttpGet]
        [Route("trading/ActiveTradesWithTrader")]
        public async Task<IActionResult> GetActiveTradesWithTrader()
        {
            // Get trades
            var activeTrades = await Globals.GlobalDataStore.GetActiveTradesAsync();

            JObject activeTradesJson = new JObject();

            // Get information for active trade
            foreach (var activeTrade in activeTrades)
            {
                activeTradesJson[activeTrade.TraderId] = JObject.FromObject(activeTrade);
            }

            return new JsonResult(activeTradesJson);
        }

        [HttpGet]
        [Route("trading/ActiveTrades")]
        public async Task<IActionResult> GetActiveTrades()
        {
            // Get trades
            var activeTrades = await Globals.GlobalDataStore.GetActiveTradesAsync();
            return new JsonResult(activeTrades);
        }

        [HttpGet]
        [Route("trading/ClosedTrades")]
        public async Task<IActionResult> GetClosedTrades()
        {
            // Get trades
            var closedTrades = await Globals.GlobalDataStore.GetClosedTradesAsync();
            return new JsonResult(closedTrades);
        }

        [HttpGet]
        [Route("trading/statistics")]
        public async Task<IActionResult> Statistics()
        {
            // Create Statistic model
            var stat = new Statistics();

            // Get winner/loser currencies
            var coins = new Dictionary<string, decimal?>();
            foreach (var cT in await Globals.GlobalDataStore.GetClosedTradesAsync())
            {
                // Get profit per currency
                if (coins.ContainsKey(cT.Market))
                    coins[cT.Market] = coins[cT.Market].Value + cT.CloseProfitPercentage;
                else
                    coins.Add(cT.Market, cT.CloseProfitPercentage);

                // Profit-loss
                if (cT.CloseProfit != null) stat.ProfitLoss = stat.ProfitLoss + cT.CloseProfit.Value;
                if (cT.CloseProfitPercentage != null)
                    stat.ProfitLossPercentage = stat.ProfitLossPercentage + cT.CloseProfitPercentage.Value;
            }

            // Coin performance
            stat.CoinPerformance = coins.ToList().OrderByDescending(c => c.Value);

            // Create some viewbags
            ViewBag.tradeOptions = Startup.Configuration.GetSection("TradeOptions").Get<TradeOptions>();
            ViewBag.stat = stat;

            return new JsonResult(ViewBag);
        }

        /// <summary>
        /// Logs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("logs")]
        public IActionResult Logs()
        {
            var log = Log.ReadTail("Logs/Mynt-" + DateTime.Now.ToString("yyyyMMdd") + ".log", 100);
            ViewBag.log = log;
            return new JsonResult(ViewBag);
        }
    }
}
