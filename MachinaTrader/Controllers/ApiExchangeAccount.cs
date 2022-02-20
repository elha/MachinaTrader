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
using MachinaTrader.Exchanges;

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
            
            try
            {
                if (fullApi.PublicApiKey.Length > 0 && fullApi.PrivateApiKey.Length > 0)
                {
                    // Get Tickers & Balances
                    var balances = await fullApi.GetAmountsAsync();

                    // Calculate stuff
                    foreach (var balance in balances)
                    {

                        // Create balanceEntry
                        var balanceEntry = new BalanceEntry()
                        {
                            DisplayCurrency = displayOptions.DisplayFiatCurrency,
                            Market = balance.Key,
                            TotalCoins = balance.Value,
                            BalanceInUsd = MarketManager.GetUSD(balance.Key, balance.Value),
                            BalanceInBtc = MarketManager.GetBTC(balance.Key, balance.Value),
                            BalanceInDisplayCurrency = MarketManager.GetConversion(balance.Key, balance.Value, displayOptions.DisplayFiatCurrency)
                        };

                        // Add to list
                        if(balanceEntry.BalanceInUsd > 5)
                            account.Positions.Add(balanceEntry);
                    }
                }
                else
                    Global.Logger.Information("No api under configuration");


                account.TotalInUsd = account.Positions.Sum(a => a.BalanceInUsd).GetValueOrDefault();
                account.TotalInBtc = account.Positions.Sum(a => a.BalanceInBtc).GetValueOrDefault();
              

            }
            catch (Exception e)
            {
                //Global.Logger.Error(e.InnerException.Message);
            }

            return account;
        }
    }
}
