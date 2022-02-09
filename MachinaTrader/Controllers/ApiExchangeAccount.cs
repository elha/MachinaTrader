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
    [AllowAnonymous, Route("api/exchange/account/")]
    public class ApiAccounts : Controller
    {
        public class BalanceModel 
        {
            public List<BalanceEntry> Positions { get; set; } = new List<BalanceEntry>();
            public decimal TotalInUsd { get; set; }
            public decimal TotalInBtc { get; set; }
        }

        /// <summary>
        /// Gets the balance for 1 Exchange
        /// Convert it so
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("balance")]
        public async Task<BalanceModel> GetBalance()
        {
            //Account
            var account = new BalanceModel();

            // Some Options
            var displayOptions = Global.Configuration.DisplayOptions;

            // Get Exchange account
            var fullApi = Global.ExchangeApi.GetFullApi().Result;
            var markets = Global.ExchangeApi.GetMarketSummaries(null).Result;
            try
            {
                if (fullApi.PublicApiKey.Length > 0 && fullApi.PrivateApiKey.Length > 0)
                {
                    // Get Tickers & Balances
                    var balances = await fullApi.GetAmountsAsync();
                    var convertBTCUSD = markets.Find(m => m.GlobalMarketName=="BTC-USD");

                    // Calculate stuff
                    foreach (var balance in balances)
                    {
                        // Get selected tickers for Balances
                        var balanceUsd = balance.Value;
                        if (balance.Key != "USD")
                        {
                            try
                            {
                                var convertUSD = markets.Find(m => m.GlobalMarketName == $"{balance.Key}-USD");
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
                            account.Positions.Add(balanceEntry);
                    }
                }
                else
                    Global.Logger.Information("No api under configuration");


                account.TotalInUsd = account.Positions.Sum(a => a.BalanceInUsd).GetValueOrDefault();
                account.TotalInBtc = account.Positions.Sum(a => a.BalanceInBtc).GetValueOrDefault();
                
                var total = new BalanceEntry()
                {
                    DisplayCurrency = displayOptions.DisplayFiatCurrency,
                    Market = "TOTAL",
                    TotalCoins = null,
                    BalanceInUsd = account.TotalInUsd,
                    BalanceInBtc = account.TotalInBtc,
                    BalanceInDisplayCurrency = account.Positions.Sum(a => a.BalanceInDisplayCurrency)
                };

                account.Positions.Add(total);
            }
            catch (Exception e)
            {
                //Global.Logger.Error(e.InnerException.Message);
            }

            return account;
        }
    }
}
