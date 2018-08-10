using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MachinaTrader.Models;
using ExchangeSharp;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.Authorization;
using Mynt.Core.Enums;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/account/")]
    public class ApiAccount : Controller
    {
        /// <summary>
        /// Gets the balance for 1 Exchange
        /// Convert it so
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        public async Task<IActionResult> GetBalance()
        {
            //Account
            var account = new List<BalanceEntry>();

            // TradeOptions
            var tradeOptions = Runtime.Configuration.TradeOptions;

            // Get Exchange account
            var fullApi = Runtime.GlobalExchangeApi.GetFullApi().Result;

            // Get Tickers
            var tickers = await fullApi.GetTickersAsync();
            if (tickers.Count() > 1)
            {
                // Get USD-QuoteCurr ticker
                var tickerUsdQuote = tickers.First(t => t.Key.ToUpper().Contains(tradeOptions.QuoteCurrency) && t.Key.ToUpper().Contains("USD") && t.Value.Last > 1);
            
                // Balances
                var balances = await fullApi.GetAmountsAsync();
                foreach (var balance in balances)
                {
                    // Get Ticker
                    var ticker = tickers.First(t => t.Key.Contains(tradeOptions.QuoteCurrency) && t.Key.Contains(balance.Key));

                    // Create balanceEntry
                    var balanceEntry = new BalanceEntry()
                    {
                        QuoteCurrrency = tradeOptions.QuoteCurrency,
                        Market = balance.Key,
                        TotalCoins = balance.Value,
                    };

                    // Market same as quoteCurrency
                    if (balanceEntry.Market.ToUpper() == tradeOptions.QuoteCurrency.ToUpper())
                    {
                        balanceEntry.BalanceValueQuoteCurrency = balance.Value;
                        balanceEntry.BalanceValueUsd = tickerUsdQuote.Value.Last * balance.Value;
                    }
                    // Market same as USD
                    else if (balanceEntry.Market.ToUpper().Contains("USD"))
                    {
                        balanceEntry.BalanceValueQuoteCurrency = balance.Value;
                        balanceEntry.BalanceValueUsd = balance.Value;
                    }
                    // Anything else
                    else
                    {
                        balanceEntry.BalanceValueQuoteCurrency = balance.Value * ticker.Value.Last;
                        balanceEntry.BalanceValueUsd = tickerUsdQuote.Value.Last * (balance.Value * ticker.Value.Last);
                    }

                    // Add to list
                    account.Add(balanceEntry);
                }
            }
            else
            {
                Global.Logger.Information("Possible problem with API");
            }

            return new JsonResult(account);
        }
    }
}
