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
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{

    [Authorize, Route("api/statistics/")]
    public class ApiStatistic : Controller
    {
        [HttpGet]
        [Authorize, Route("overview")]
        public async Task<IActionResult> Statistics(string mode = "paper", DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Check mode
            var paperTrade = mode != "live";
            
            // Create Statistic model
            var stat = new Statistics();

            // Get closed trades
            var closedTrades = await Global.DataStore.GetClosedTradesAsync();
            var closedTradesClean = closedTrades.Where(c => c.CloseDate != null &&
                                                            (fromDate != null && toDate != null &&
                                                             (c.SellOrderId != null &&
                                                                c.PaperTrade == paperTrade &&
                                                                c.CloseDate.Value.Date >= fromDate.Value.Date &&
                                                                c.CloseDate.Value.Date <= toDate.Value.Date)));

            // Coins Profit-loss
            var sortedList = closedTradesClean.GroupBy(x => x.Market);

            var coins = sortedList.Select(coinGroup => new CoinPerformance()
            {
                Coin = coinGroup.Key,
                InvestedCoins = coinGroup.Sum(x => x.StakeAmount),
                Performance = coinGroup.Sum(x => x.CloseProfit),
                //TODO performance korrekt berechnen
                PerformancePercentage = coinGroup.Sum(x => x.CloseProfitPercentage),
                PositiveTrades = coinGroup.Count(c => c.CloseProfit > 0),
                NegativeTrade = coinGroup.Count(c => c.CloseProfit < 0)
            }).ToList();

            // Invested amout
            stat.InvestedCoins = coins.Sum(c => c.InvestedCoins);

            // General Profit-loss
            stat.ProfitLoss = coins.Sum(c => c.Performance);
            stat.ProfitLossPercentage = coins.Sum(c => c.PerformancePercentage);

            // Coin performance
            stat.CoinPerformances = coins;

            // Create some viewbags
            ViewBag.tradeOptions = Global.Configuration.TradeOptions;
            ViewBag.stat = stat;

            return new JsonResult(ViewBag);
        }

        [HttpGet]
        [Route("wallet")]
        public async Task<IActionResult> Wallet()
        {
            var stat = new WalletStatistic();

            var items = await Global.DataStore.GetWalletTransactionsAsync();
            stat.Dates = items.Select(i => i.Date).ToList();
            stat.Amounts = items.Select(i => i.Amount).ToList();

            var balances = new List<decimal>();

            for (int i = 0; i < items.Count; i++)
            {
                decimal balance = Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationStartingWallet;
                for (int j = i; j >= 0; j--)
                {
                    balance = balance + items[j].Amount;
                }
                balances.Add(balance);
            }

            stat.Balances = balances;
            ViewBag.stat = stat;

            return new JsonResult(ViewBag);
        }

    }
}
