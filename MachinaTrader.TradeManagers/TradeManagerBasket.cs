using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MachinaTrader.Strategies;

namespace MachinaTrader.TradeManagers
{
    // tries to buy/sell within basket
    public class TradeManagerBasket : ITradeManager
    {
        private static Dictionary<string, decimal> mQuotesAtLastBuy = new Dictionary<string, decimal>();
        private int mStartuplastBuyMinutes = -45;
        private const Decimal mThresholdPercent = 0.8m;
        private const Decimal mThresholdDeductionPerHour = 0.1m;
        private static DateTime? mDateAtLastBuy;
        private static decimal mAmountToReinvestRemains = 99.7m;
        public List<MarketSummary> mMarkets { get; private set; }

        /// <summary>
        /// Checks if new trades can be started.
        /// </summary>
        /// <returns></returns>
        public async Task LookForNewTrades(string strategyString = null)
        {
            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation) return;
            if (Global.Configuration.TradeOptions.PaperTrade) return;

            await PrepareRun();
            await FindOpportunities();
        }

        public async Task UpdateExistingTrades()
        {
            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation) return;
            if (Global.Configuration.TradeOptions.PaperTrade) return;

            await UpdateOpenOrders();
        }


        private async Task PrepareRun()
        {
            // fetch symbols and resulting markets
            var Symbols = Global.Configuration.TradeOptions.TradeAssetsList().ToList();
            Symbols.Add(Global.Configuration.TradeOptions.QuoteCurrency);

            this.mMarkets = Global.ExchangeApi.GetMarketSummaries(null).Result.Where(m =>
                Symbols.Any(c => c == m.CurrencyPair.BaseCurrency) && Symbols.Any(c => c == m.CurrencyPair.QuoteCurrency)
            ).ToList();

            // first run fetch old Tickers
            if (!mDateAtLastBuy.HasValue)
            {
                mDateAtLastBuy = DateTime.Now.AddMinutes(mStartuplastBuyMinutes);
            }

            foreach (var market in this.mMarkets)
            {
                if (!mQuotesAtLastBuy.ContainsKey(market.GlobalMarketName))
                {
                    var oldquotes = await Global.ExchangeApi.GetTickerHistory(market.GlobalMarketName, Period.Minute, -mStartuplastBuyMinutes);
                    mQuotesAtLastBuy[market.GlobalMarketName] = oldquotes.FirstOrDefault().Close;
                }
            }
        }

        /// <summary>
        /// Checks the implemented trading indicator(s),
        /// if one pair triggers the buy signal a new trade record gets created.
        /// </summary>
        /// <returns></returns>
        private async Task FindOpportunities()
        {

            var api = Global.ExchangeApi.GetFullApi();

            // calc Threshold
            var dTargetThreshold = mThresholdPercent;
            var nLastTradeAgeInHours = (decimal)(DateTime.Now - mDateAtLastBuy.Value).TotalHours;
            if (nLastTradeAgeInHours > 1) dTargetThreshold -= mThresholdDeductionPerHour * nLastTradeAgeInHours;


            // check every market if threshold met, sort by biggest change
            var bTradeFound = false;
            var curquotes = new Dictionary<string, decimal>();
            foreach (var market in mMarkets.Distinct().OrderByDescending(x => Math.Abs((x.Bid - mQuotesAtLastBuy[x.GlobalMarketName]) / mQuotesAtLastBuy[x.GlobalMarketName])).ToList())
            {
                var curquote = market.Bid;
                if (curquote == 0) continue;
                curquotes[market.GlobalMarketName] = curquote;

                var buyquote = mQuotesAtLastBuy[market.GlobalMarketName];

                var change = (curquote - buyquote) / buyquote * 100m;

                if (Math.Abs(change) < dTargetThreshold) continue;

                var amountQuoteCurrency = 0m;
                var amountBaseCurrency = 0m;
                var amountUsd = 0m;

                OrderSide side = OrderSide.Buy;
                string orderId = null;
                if (change < 0)
                {
                    // buy
                    side = OrderSide.Buy;
                    var balance = await Global.ExchangeApi.GetBalance(market.CurrencyPair.QuoteCurrency);
                    var usd = GetUSD(balance);
                    if (usd < 50m) continue;

                    var nPercent = Global.Configuration.TradeOptions.AmountToReinvestPercentage;
                    if (usd < 800m) nPercent = 100m - 0.26m; // all minus fee

                    amountQuoteCurrency = balance.Available * nPercent / 100m;
                    amountBaseCurrency = amountQuoteCurrency / curquote; // balance is in QuoteCurrency
                    amountUsd = GetUSD(market.CurrencyPair.BaseCurrency, amountBaseCurrency);
                    orderId = await Global.ExchangeApi.Buy(market.GlobalMarketName, amountBaseCurrency, curquote);
                    Global.Logger.Information($"Found BUY  SIGNAL for: {market.GlobalMarketName} at {curquote} (sold @ {buyquote}, available {balance.Available} {balance.Currency})");
                }
                else
                {
                    // sell
                    side = OrderSide.Sell;
                    var balance = await Global.ExchangeApi.GetBalance(market.CurrencyPair.BaseCurrency);
                    var usd = GetUSD(balance);
                    if (usd < 50m) continue;

                    var nPercent = Global.Configuration.TradeOptions.AmountToReinvestPercentage;
                    if (usd < 800m) nPercent = 100m;

                    amountBaseCurrency = balance.Available * nPercent / 100m;
                    amountQuoteCurrency = amountBaseCurrency * curquote; // balance is in QuoteCurrency
                    amountUsd = GetUSD(market.CurrencyPair.BaseCurrency, amountBaseCurrency);
                    orderId = await Global.ExchangeApi.Sell(market.GlobalMarketName, amountBaseCurrency, curquote);
                    Global.Logger.Information($"Found SELL SIGNAL for: {market.GlobalMarketName} at {curquote} (bought @ {buyquote}, available {balance.Available} {balance.Currency})");
                }


                if (orderId == null)
                {
                    Global.Logger.Error($"Error to open a {side} Order for: {market.GlobalMarketName} {amountBaseCurrency} {curquote}");
                    return;
                }

                bTradeFound = true;

                var fullApi = await Global.ExchangeApi.GetFullApi();
                var trade = new Trade()
                {
                    GlobalSymbol = market.GlobalMarketName,
                    Market = market.GlobalMarketName,

                    StakeAmount = amountQuoteCurrency,

                    OpenDate = DateTime.Now,
                    OpenRate = curquote,
                    Quantity = amountBaseCurrency,

                    OpenOrderId = orderId,
                    BuyOrderId = side == OrderSide.Buy ? orderId : "",
                    SellOrderId = side == OrderSide.Sell ? orderId : "",

                    IsOpen = true,
                    IsBuying = side == OrderSide.Buy,
                    IsSelling = side == OrderSide.Sell,

                    StrategyUsed = "Basket",
                    SellType = SellType.None,
                    Exchange = fullApi.Name,
                    PaperTrade = false
                };


                Global.Logger.Information($"Opened a {side} Order for: {this.TradeToString(trade)}");

                // Save the order.
                await Global.DataStore.SaveTradeAsync(trade);

                // Send a notification that we found something suitable
                await SendNotification($"Saved a {side} ORDER for: {this.TradeToString(trade)}");

            };

            if (bTradeFound)
            {
                mQuotesAtLastBuy = curquotes;
                mDateAtLastBuy = DateTime.Now;
            }
        }

        public decimal GetUSD(string Currency, decimal value)
        {
            if (Currency == "USD") return value;
            var market = mMarkets.Find(m => m.CurrencyPair.BaseCurrency == Currency && m.CurrencyPair.QuoteCurrency == "USD");
            return market.Bid * value;
        }

        public decimal GetUSD(AccountBalance balance)
        {
            return GetUSD(balance.Currency, balance.Available);
        }

        public decimal GetBTC(string Currency, decimal value)
        {
            if (Currency == "BTC" || Currency == "XBT") return value;
            var market = mMarkets.Find(m => m.CurrencyPair.BaseCurrency == Currency && m.CurrencyPair.QuoteCurrency == "BTC");
            return market.Bid * value;
        }

        public decimal GetBTC(AccountBalance balance)
        {
            return GetBTC(balance.Currency, balance.Available);
        }



        private async Task UpdateOpenOrders()
        {
            // First we update our open buy orders by checking if they're filled.
            var activeTrades = await Global.DataStore.GetActiveTradesAsync();

            // Secondly we check if currently selling trades can be marked as sold if they're filled.
            foreach (var trade in activeTrades)
            {
                var dir = trade.IsBuying ? "BUY" : "SELL";
                Global.Logger.Information($"Order Checking Status {dir} {this.TradeToString(trade)}");
                if (trade.IsOpen)
                    new Thread(async () => await UpdateOpenOrder(trade)).Start();
            }
        }

        /// <summary>
        /// Updates the sell orders by checking with the exchange what status they are currently.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateOpenOrder(Trade trade)
        {
            try
            {
                var exchangeOrder = await Global.ExchangeApi.GetOrder(trade.BuyOrderId ?? trade.SellOrderId, trade.Market);
                if (exchangeOrder?.Status == OrderStatus.Filled)
                {
                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsSelling = false;
                    trade.IsBuying = false;

                    trade.CloseDate = exchangeOrder.OrderDate;
                    trade.CloseRate = exchangeOrder.Price;
                    trade.Quantity = exchangeOrder.ExecutedQuantity;

                    trade.CloseProfit = (exchangeOrder.Price * exchangeOrder.ExecutedQuantity) - trade.StakeAmount;
                    if (trade.StakeAmount!=0m)
                        trade.CloseProfitPercentage = ((exchangeOrder.Price * exchangeOrder.ExecutedQuantity) - trade.StakeAmount) / trade.StakeAmount * 100;

                    await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                    {
                        Amount = (trade.CloseRate.Value * trade.Quantity),
                        Date = trade.CloseDate.Value
                    });

                    await SendNotification($"Order is filled: {this.TradeToString(trade)}");
                }
                else if (exchangeOrder?.Status == OrderStatus.PartiallyFilled)
                {
                    //wait
                    return;
                }
                else
                {
                    if ((trade.OpenDate.AddSeconds(Global.Configuration.TradeOptions.MaxOpenTimeBuy) > DateTime.UtcNow))
                    {
                        await SendNotification($"Order wasn't filled: {this.TradeToString(trade)} waiting until {trade.OpenDate.AddSeconds(Global.Configuration.TradeOptions.MaxOpenTimeBuy)}");
                        return;
                    }
                    await Global.ExchangeApi.CancelOrder(trade.BuyOrderId ?? trade.SellOrderId, trade.Market);

                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsSelling = false;
                    trade.IsBuying = false;

                    await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                    {
                        Amount = (trade.OpenRate * trade.Quantity),
                        Date = DateTime.UtcNow
                    });

                    await SendNotification($"Order cancelled because it wasn't filled in time: {this.TradeToString(trade)}.");
                    Global.Logger.Information($"Order Canceled {trade.BuyOrderId ?? trade.SellOrderId}");
                }
            }
            catch (ExchangeSharp.APIException ex)
            {
                if (ex.Message.ToLower().Contains("invalid order") || (ex.Message.ToLower().Contains("unkown order")))
                {
                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsSelling = false;
                    trade.IsBuying = false;
                    Global.Logger.Information($"Order Notfound {trade.BuyOrderId ?? trade.SellOrderId}");
                }
                else
                    throw ex;
            }
            finally
            {
                // update our database.
                await Global.DataStore.SaveTradeAsync(trade);
            }

        }

        private async Task SendNotification(string message)
        {
            Global.Logger.Debug(message);

            if (Global.NotificationManagers != null)
            {
                foreach (var notificationManager in Global.NotificationManagers)
                {
                    await notificationManager.SendNotification(message);
                }
            }
        }

        private string TradeToString(Trade trade)
        {
            return string.Format($"#{trade.Market} with limit {trade.OpenRate:0.00000000} {Global.Configuration.TradeOptions.QuoteCurrency} " +
                                 $"({trade.Quantity:0.0000} {trade.GlobalSymbol.Replace(Global.Configuration.TradeOptions.QuoteCurrency, "")} " +
                                 $"{trade.OpenDate} " +
                                 $"({trade.TradeId})");
        }
    }
}
