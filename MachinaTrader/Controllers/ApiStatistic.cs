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
using MachinaTrader.Exchanges;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.TradeManagers;
using static MachinaTrader.Exchanges.MarketManager;

namespace MachinaTrader.Controllers
{

    [AllowAnonymous, Route("api/statistics/")]
    public class ApiStatistic : Controller
    {
        [HttpGet]
        [Route("overview")]
        public async Task<ApiStatResult> Statistics()
        {
            var _ = new ApiStatResult();
            var fullApi = Global.ExchangeApi.GetFullApi();
            try
            {
                if (fullApi.PublicApiKey.Length > 0 && fullApi.PrivateApiKey.Length > 0)
                {
                    var balances = await fullApi.GetAmountsAsync();
                    _.BalanceUsd = Math.Round(balances.Sum(b => MarketManager.GetUSD(b.Key, b.Value)), 2);
                    _.BalanceBtc = Math.Round(balances.Sum(b => MarketManager.GetBTC(b.Key, b.Value)), 5);
                }

                _.GlobalMarketTrend = MarketManager.GlobalTrend.ToString();
                _.GlobalMarketTrendScore = MarketManager.GlobalTrendScore;
                _.ActiveTrades = TradeManagerBasket.ActiveTrades;
                _.RiskValue = Math.Round(DepotManager.RiskValue, 2);

                var closedTrades = await Global.DataStore.GetClosedTradesAsync(DateTime.UtcNow.AddHours(-24));
                _.PerformanceSum = Math.Round(closedTrades.Sum(t => t.TradePerformance), 2) * 10m;
                if (closedTrades.Count() > 0)
                    _.PerformanceAvg = Math.Round(closedTrades.Average(t => t.TradePerformance), 2) * 10m;
                if (closedTrades.Count() > 0)
                    _.PositiveTrades = Math.Round((decimal)closedTrades.Count(t => t.TradePerformance > 0) / (decimal)closedTrades.Count() * 100m, 2);
            }
            catch (Exception e)
            {
            }

            return _;
        }
        public class ApiStatResult
        {
            public decimal BalanceUsd { get; internal set; }
            public decimal BalanceBtc { get; internal set; }
            public string GlobalMarketTrend { get; internal set; }
            public decimal GlobalMarketTrendScore { get; internal set; }
            public decimal RiskValue { get; internal set; }
            public decimal PerformanceSum { get; internal set; }
            public decimal PerformanceAvg { get; internal set; }
            public decimal PositiveTrades { get; internal set; }
            public int ActiveTrades { get; internal set; }
        }


    }
}
