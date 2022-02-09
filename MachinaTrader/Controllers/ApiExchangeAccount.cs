using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MachinaTrader.Models;
using ExchangeSharp;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.Authorization;
using MachinaTrader.TradeManagers;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/exchange/account/")]
    public class ApiAccounts : Controller
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

            // Some Options
            var displayOptions = Global.Configuration.DisplayOptions;

            // Get Exchange account
            var fullApi = Global.ExchangeApi.GetFullApi().Result;

            try
            {
                if (fullApi.PublicApiKey.Length > 0 && fullApi.PrivateApiKey.Length > 0)
                {
                    // Get Tickers & Balances
                    var balances = await fullApi.GetAmountsAsync();
                    var convertBTCUSD = await Global.ExchangeApi.GetTicker("BTC-USD");

                    // Calculate stuff
                    foreach (var balance in balances)
                    {
                        // Get selected tickers for Balances
                        var balanceUsd = balance.Value;
                        if (balance.Key != "USD")
                        {
                            try
                            {
                                var convertUSD = await Global.ExchangeApi.GetTicker(balance.Key + "-USD");
                                balanceUsd *= convertUSD.Bid;
                            }
                            catch (Exception ex) { }
                        }


                        // Create balanceEntry
                        var balanceEntry = new BalanceEntry()
                        {
                            DisplayCurrency = displayOptions.DisplayFiatCurrency,
                            Market = balance.Key,
                            TotalCoins = balance.Value,
                            BalanceInUsd = balanceUsd,
                            BalanceInBtc = balanceUsd / convertBTCUSD.Bid,
                            BalanceInDisplayCurrency = balanceUsd
                        };

                        // Add to list
                        if(balanceUsd>5)
                            account.Add(balanceEntry);
                    }
                }
                else
                    Global.Logger.Information("No api under configuration");


                var total = new BalanceEntry()
                {
                    DisplayCurrency = displayOptions.DisplayFiatCurrency,
                    Market = "TOTAL",
                    TotalCoins = null,
                    BalanceInUsd = account.Sum(a => a.BalanceInUsd),
                    BalanceInBtc = account.Sum(a => a.BalanceInBtc),
                    BalanceInDisplayCurrency = account.Sum(a => a.BalanceInDisplayCurrency)
                };
                account.Add(total);
            }
            catch (Exception e)
            {
                //Global.Logger.Error(e.InnerException.Message);
            }

            return new JsonResult(account);
        }
    }
}
