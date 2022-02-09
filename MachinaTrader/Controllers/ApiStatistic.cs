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
using Trade = MachinaTrader.Globals.Structure.Models.Trade;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Controllers
{

    [Authorize, Route("api/statistics/")]
    public class ApiStatistic : Controller
    {
        [HttpGet]
        [Authorize, Route("overview")]
        public async Task<IActionResult> Statistics(string mode = "paper", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var tradeOptions = Global.Configuration.TradeOptions;

            // Check mode
            var paperTrade = mode != "live";

            // Create Statistic model
            var stat = new Statistics();

            // Get closed trades
            var closedTrades = await Global.DataStore.GetClosedTradesAsync();
            IEnumerable<Trade> closedTradesClean;
            if (fromDate != null && toDate != null)
                closedTradesClean = closedTrades.Where(c =>
                    c.CloseDate != null && (c.SellOrderId != null && c.PaperTrade == paperTrade &&
                                            c.CloseDate.Value.Date >= fromDate.Value.Date &&
                                            c.CloseDate.Value.Date <= toDate.Value.Date));
            else
                closedTradesClean = closedTrades.Where(c => c.CloseDate != null && c.SellOrderId != null && c.PaperTrade == paperTrade);

            // Coins Profit-loss
            var sortedList = closedTradesClean.GroupBy(x => x.Market);

            var coins = sortedList.Select(coinGroup => new CoinPerformance()
            {
                Coin = coinGroup.Key,
                InvestedCoins = coinGroup.Sum(x => x.StakeAmount),
                Performance = coinGroup.Sum(x => x.CloseProfit),
                PerformancePercentage = coinGroup.Sum(x => x.CloseProfitPercentage),
                PositiveTrades = coinGroup.Count(c => c.CloseProfit > 0),
                NegativeTrades = coinGroup.Count(c => c.CloseProfit < 0)
            }).ToList();

            // General Profit-loss
            stat.ProfitLoss = coins.Sum(c => c.Performance);
            stat.ProfitLossPercentage = coins.Sum(c => c.PerformancePercentage);
            var StrtAmount = 1000m;

            // Invested amout
            stat.InvestedCoins = coins.Sum(c => c.InvestedCoins);
            if ((StrtAmount * stat.ProfitLoss == 0))
            {
                stat.InvestedCoinsPerformance = 0;
            }
            else
            {
                stat.InvestedCoinsPerformance = ((StrtAmount * stat.ProfitLoss) / 100) * 100;
            }

            // Coin performance
            stat.CoinPerformances = coins;

            // Trades amount
            stat.PositiveTrades = coins.Sum(c => c.PositiveTrades);
            stat.NegativeTrades = coins.Sum(c => c.NegativeTrades);

            // Balances
            stat.CurrentBalance = StrtAmount + stat.ProfitLoss;
            stat.CurrentBalancePerformance = ((stat.ProfitLoss) * 100) / StrtAmount;


            return new JsonResult(new ApiStatResult() { TradeOptions = tradeOptions, Stat = stat });
        }
        public class ApiStatResult
        {
            public TradeOptions TradeOptions { get; set; }
            public Statistics Stat { get; set; }

        }

        [HttpGet]
        [Authorize, Route("overviewChart")]
        public async Task<IActionResult> StatisticsChart(string mode = "paper", bool includeStartAmount = false, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Chart Model
            var stat = new StatisticChart();

            var tradeOptions = Global.Configuration.TradeOptions;

            // Check mode
            var paperTrade = mode != "live";


            // Get closed trades
            var closedTrades = await Global.DataStore.GetClosedTradesAsync();
            IEnumerable<Trade> closedTradesClean;
            if (fromDate != null && toDate != null)
                closedTradesClean = closedTrades.Where(c =>
                    c.CloseDate != null && (c.SellOrderId != null && c.PaperTrade == paperTrade &&
                                            c.CloseDate.Value.Date >= fromDate.Value.Date &&
                                            c.CloseDate.Value.Date <= toDate.Value.Date));
            else
                closedTradesClean = closedTrades.Where(c => c.CloseDate != null && c.SellOrderId != null && c.PaperTrade == paperTrade);

            // Get first trade date
            var tradesClean = closedTradesClean.ToList();
            var firstTradeDate = tradesClean.Select(x => x.CloseDate).Min();

            // iterate through dates and calculate balance
            var balances = new List<decimal>();
            var positiveT = new List<decimal>();
            var negativeT = new List<decimal>();

            // Generate all dates & balances
            var balance = 0m;
            stat.Dates = new List<DateTime>();
            if (firstTradeDate != null)
                for (var dt = firstTradeDate.Value; dt <= DateTime.Today; dt = dt.AddDays(1))
                {
                    // add dates
                    stat.Dates.Add(dt);

                    // Get trades
                    var trades = tradesClean.Where(t => t.CloseDate != null && t.CloseDate.Value.Date == dt.Date);
                    var posT = trades.Count(t => t.CloseProfit > 0);
                    positiveT.Add(posT);
                    var negT = trades.Count(t => t.CloseProfit < 0);
                    negativeT.Add(negT * -1);

                    // Get balances
                    var sumTrades = trades.Sum(x => x.CloseProfit);
                    if (sumTrades != null) balance += sumTrades.Value;
                    balances.Add(balance);
                }
            var o = new ApiChartResult();
            stat.NegativeTrades = negativeT;
            stat.PositiveTrades = positiveT;
            stat.Balances = balances;
            o.Stat = stat;

            return new JsonResult(o);
        }
        public class ApiChartResult
        {
            public StatisticChart Stat { get; internal set; }
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
            var exchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();

            for (int i = 0; i < items.Count; i++)
            {
                decimal balance = exchangeOption.SimulationStartingWallet;
                for (int j = i; j >= 0; j--)
                {
                    balance = balance + items[j].Amount;
                }
                balances.Add(balance);
            }
            var o = new ApiWalletResult();
            stat.Balances = balances;
            o.Stat = stat;

            return new JsonResult(o);
        }
        public class ApiWalletResult
        {
            public WalletStatistic Stat { get; internal set; }
        }
    }
}
