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
using MachinaTrader.Indicators;
using MachinaTrader.Exchanges;

namespace MachinaTrader.TradeManagers
{
    // tries to buy/sell within basket
    public class TradeManagerBasket : ITradeManager
    {
        /// <summary>
        /// Checks if new trades can be started.
        /// </summary>
        /// <returns></returns>
        public async Task LookForNewTrades(string strategyString = null)
        {
            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation) return;
            if (Global.Configuration.TradeOptions.PaperTrade) return;

            MarketManager.Update();
            MarketManager.Strategy = StrategyFactory.GetTradingStrategy(Global.Configuration.TradeOptions.DefaultStrategy);
            MarketManager.Strategy.Parameters = Global.Configuration.TradeOptions.DefaultStrategyParameters;
            DepotManager.Update();
            var trades = await UpdateOpenPositions();
            FindOpportunities(trades);
            MarketManager.SaveToDB();
        }

        public async Task UpdateExistingTrades()
        {
 
        }

        private async Task<List<Trade>> UpdateOpenPositions()
        {
            // update our open orders
            var activeTrades = Global.DataStore.GetActiveTradesAsync().Result;
            foreach (var trade in activeTrades)
            {

                if (!trade.IsBuying && !trade.IsSelling)
                {
                    // should be sold?
                    var m = MarketManager.Markets[trade.GlobalSymbol];
                    trade.TickerLast = m.LastTicker;

                    var advice = m.GetSellAdvice(trade);

                    if (advice.Advice == TradeAdviceEnum.Sell)
                    {
                        Global.Logger.Information($"Signal {advice.Advice} {m.GlobalMarketName} {MarketManager.Strategy.Name}({advice.Comment})");
                        await ExecuteTrade(m, trade, advice);
                    }

                }
                if(trade.TickerLast == null)
                {
                    var m = MarketManager.Markets[trade.GlobalSymbol];
                    trade.TickerLast = m.LastTicker;
                }

                new Thread(async () =>
                {
                    
                    await UpdateOpenOrder(trade);
                    await Global.DataStore.SaveTradeAsync(trade);
                }).Start();
            }
            return activeTrades;
        }

        /// <summary>
        /// Checks the implemented trading indicator(s),
        /// if one pair triggers the buy signal a new trade record gets created.
        /// </summary>
        /// <returns></returns>
        private async void FindOpportunities(List<Trade> trades)
        {
            foreach (var m in MarketManager.Markets.Values.Where(m => m.Active))
            {
                var advice = m.GetBuyAdvice();

                if (advice.Advice == TradeAdviceEnum.Buy && !trades.Any(t => t.GlobalSymbol == m.GlobalMarketName)) // && !DepotManager.HasPosition(m.CurrencyPair.BaseCurrency) && DepotManager.HasPosition(m.SettleCurrency))
                {
                    Global.Logger.Information($"Signal {advice.Advice} {m.GlobalMarketName} {MarketManager.Strategy.Name}({advice.Comment})");
                    await ExecuteTrade(m, null, advice);
                }
            }
        }


        private async Task ExecuteTrade(TradeMarket Market, Trade trade, TradeAdvice advice)
        {
            var side = advice.Advice == TradeAdviceEnum.Buy ? OrderSide.Buy : OrderSide.Sell;
            var amountQuoteCurrency = 0m;
            var amountBaseCurrency = 0m;
            string orderId = null;

            // buy
            if (side == OrderSide.Buy)
            {
                var currency = Market.SettleCurrency;
                var fee = 0.0005m;

                var amountSettleCurency = DepotManager.GetPositionSize(currency, fee);
                if (Market.CurrencyPair.QuoteCurrency == "USD" || Market.CurrencyPair.QuoteCurrency == "USDT")
                    amountQuoteCurrency = MarketManager.GetUSD(currency, amountSettleCurency);
                if (Market.CurrencyPair.QuoteCurrency == "BTC")
                    amountQuoteCurrency = MarketManager.GetBTC(currency, amountSettleCurency);

                amountBaseCurrency = Math.Floor((amountQuoteCurrency / Market.Last.Close) / Market.LotSize) * Market.LotSize;

                if (amountBaseCurrency == 0) return;
                orderId = Global.ExchangeApi.Buy(Market.GlobalMarketName, amountBaseCurrency, Market.Last.Close).Result;
            }

            // sell
            if (side == OrderSide.Sell)
            {
                amountBaseCurrency = trade.Quantity;
                amountQuoteCurrency = amountBaseCurrency * Market.Last.Close; // balance is in QuoteCurrency

                if (amountBaseCurrency == 0) return;
                orderId = Global.ExchangeApi.Sell(Market.GlobalMarketName, amountBaseCurrency, Market.Last.Close).Result;
            }

            var arrow = (side == OrderSide.Buy) ? "<<" : ">>";
            Global.Logger.Information($"Order {side} {Market.GlobalMarketName} @ {Market.Last.Close} {amountBaseCurrency}{Market.CurrencyPair.BaseCurrency} {arrow} {amountQuoteCurrency}{Market.CurrencyPair.QuoteCurrency} Signal {advice.Comment}");

            if (orderId == null)
            {
                Global.Logger.Error($"Error Order {side} {Market.GlobalMarketName}, terminate");
                return;
            }

            var stake = MarketManager.GetUSD(Market.CurrencyPair.BaseCurrency, amountBaseCurrency);

            if (side == OrderSide.Buy)
            {
                trade = new Trade()
                {
                    GlobalSymbol = Market.GlobalMarketName,
                    Market = Market.GlobalMarketName,
                };
                trade.Exchange = "";
                trade.PaperTrade = false;
                trade.StrategyUsed = MarketManager.Strategy.Name;

                trade.StakeAmount = stake;
                trade.Quantity = amountBaseCurrency;

                trade.OpenDate = DateTime.Now;
                trade.OpenRate = Market.Last.Close;
                trade.OpenOrderId = orderId;
                trade.BuyOrderId = orderId;
                trade.BuyType = BuyType.Strategy;
                trade.IsOpen = true;
                trade.IsBuying = true;

                trade.TickerLast = Market.LastTicker;
                Global.Logger.Information($"PosOpen {trade.GlobalSymbol} @ {trade.OpenDate.ToString("HH:mm:ss")} {trade.OpenRate}");
            }

            if (side == OrderSide.Sell)
            {
                trade.CloseDate = DateTime.Now;
                trade.CloseRate = Market.Last.Close;
                trade.OpenOrderId = orderId;
                trade.SellOrderId = orderId;
                trade.SellType = SellType.Strategy;
                trade.IsOpen = true;
                trade.IsSelling = true;

                trade.TickerLast = Market.LastTicker;

                // preliminary
                trade.CloseProfit = stake - trade.StakeAmount;
                if (trade.StakeAmount != 0m)
                    trade.CloseProfitPercentage = trade.CloseProfit / trade.StakeAmount * 100;

                Global.Logger.Information($"PosClose {trade.GlobalSymbol} @ {trade.OpenDate.ToString("HH:mm:ss")} {trade.CloseDate?.ToString("HH:mm:ss")} {trade.OpenRate} {trade.CloseRate} {trade.CloseProfitPercentage}");
            }

            // Save the order.
            await Global.DataStore.SaveTradeAsync(trade);

        }




        /// <summary>
        /// Updates the sell orders by checking with the exchange what status they are currently.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateOpenOrder(Trade trade)
        {
            try
            {
                if (!trade.IsBuying && !trade.IsSelling) return;

                var exchangeOrder = await Global.ExchangeApi.GetOrder(trade.OpenOrderId, trade.Market);
                if (exchangeOrder?.Status == OrderStatus.Filled)
                {


                    trade.OpenOrderId = null;

                    if(trade.IsBuying)
                    {
                        trade.IsBuying = false;

                        trade.OpenDate = exchangeOrder.OrderDate;
                        if(exchangeOrder.Price.HasValue && exchangeOrder.Price > 0) trade.OpenRate = exchangeOrder.Price.Value;
                        trade.Quantity = exchangeOrder.ExecutedQuantity;

                        Global.Logger.Information($"Update Buy  {trade.GlobalSymbol} {trade.OpenDate.ToString("HH:mm:ss")}@{trade.OpenRate}");
                    }

                    if (trade.IsSelling)
                    {
                        trade.IsOpen = false;
                        trade.IsSelling = false;

                        trade.CloseDate = exchangeOrder.OrderDate;
                        if (exchangeOrder.Price.HasValue && exchangeOrder.Price > 0) trade.CloseRate = exchangeOrder.Price.Value;

                        var market = MarketManager.Markets[trade.GlobalSymbol];
                        var stake = MarketManager.GetUSD(market.CurrencyPair.QuoteCurrency, trade.CloseRate.Value * trade.Quantity);
                        trade.CloseProfit = stake - trade.StakeAmount;
                        if (trade.StakeAmount != 0m)
                            trade.CloseProfitPercentage = trade.CloseProfit / trade.StakeAmount * 100;

                        trade.Quantity = exchangeOrder.ExecutedQuantity;

                        await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                        {
                            Amount = (trade.CloseRate.Value * trade.Quantity),
                            Date = trade.CloseDate.Value
                        });

                        Global.Logger.Information($"Update Sell {trade.GlobalSymbol} {trade.OpenDate.ToString("HH:mm:ss")}@{trade.OpenRate} {trade.CloseDate?.ToString("HH:mm:ss")}@{trade.CloseRate} {trade.CloseProfitPercentage}");

                    }

                }
                else if (exchangeOrder?.Status == OrderStatus.PartiallyFilled)
                {
                    //wait
                    return;
                }
                else if (trade.IsBuying)
                {
                    if ((trade.OpenDate.AddSeconds(Global.Configuration.TradeOptions.MaxOpenTimeBuy) > DateTime.UtcNow))
                    {
                        return;
                    }

                    await Global.ExchangeApi.CancelOrder(trade.OpenOrderId, trade.Market);

                    Global.Logger.Information($"Order Buy canceled by timeout {trade.OpenOrderId}");

                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsBuying = false;

                    await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                    {
                        Amount = (trade.OpenRate * trade.Quantity),
                        Date = DateTime.UtcNow
                    });

                }
            }
            catch (ExchangeSharp.APIException ex)
            {
                if (ex.Message.ToLower().Contains("invalid order") || (ex.Message.ToLower().Contains("unkown order")))
                {
                    Global.Logger.Information($"Order Notfound {trade.OpenOrderId}");
                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsSelling = false;
                    trade.IsBuying = false;
                }
                else
                    throw ex;
            }
            finally
            {
            }

        }
    }
}
