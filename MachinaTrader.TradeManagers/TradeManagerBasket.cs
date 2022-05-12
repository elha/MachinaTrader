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
        public static List<Trade> trades;
        public static int ActiveTrades;

        /// <summary>
        /// Checks if new trades can be started.
        /// </summary>
        /// <returns></returns>
        public async Task Run(string strategyString = null)
        {
            if (Global.Configuration.ExchangeOptions.FirstOrDefault().IsSimulation) return;

            MarketManager.StrategyUp = StrategyFactory.GetTradingStrategy(Global.Configuration.TradeOptions.DefaultUpStrategy);
            MarketManager.StrategySide = StrategyFactory.GetTradingStrategy(Global.Configuration.TradeOptions.DefaultSideStrategy);
            MarketManager.Update();
            DepotManager.Update();

            trades = await Global.DataStore.GetActiveTradesAsync();
            trades = trades.OrderBy(t => t.RiskValue).ToList();
            ActiveTrades = trades.Count;
            UpdateOpenPositions();

            FindOpportunities();

            MarketManager.SaveToDB();
        }

        private async void UpdateOpenPositions()
        {
            var riskcapital = 0m;
            // update our open orders

            foreach (var trade in trades)
            {
                try
                {

                    if (!trade.IsBuying && !trade.IsSelling)
                    {
                        // should be sold?
                        var m = MarketManager.Markets[trade.GlobalSymbol];
                        trade.TickerLast = m.LastTicker;
                        if (trade.TickerMin == null || trade.TickerLast.Mid() < trade.TickerMin.Mid()) trade.TickerMin = m.LastTicker;
                        if (trade.TickerMax == null || trade.TickerLast.Mid() > trade.TickerMax.Mid()) trade.TickerMax = m.LastTicker;
                        var advice = m.GetSellAdvice(trade);

                        if (trade.TradePerformance < -DepotManager.MaxRiskPercentage)
                        {
                            // Riskmanager
                            advice.Advice = TradeAdviceEnum.Sell;
                            advice.SellType = SellType.Cancelled;
                            advice.Comment = $"ESTOP  Risk {riskcapital:N2} > {DepotManager.MaxRiskValue:N2} too high";
                            await ExecuteTrade(m, trade, advice);
                        }
                        else
                        if (advice.Advice == TradeAdviceEnum.Sell && trade.PositionType == PositionType.Long)
                        {
                            await ExecuteTrade(m, trade, advice);
                        }
                        else
                        if (advice.Advice == TradeAdviceEnum.Buy && trade.PositionType == PositionType.Short)
                        {
                            advice.Advice = TradeAdviceEnum.Sell;
                            await ExecuteTrade(m, trade, advice);
                        }
                    }

                    if (trade.TickerLast == null)
                    {
                        var m = MarketManager.Markets[trade.GlobalSymbol];
                        trade.TickerLast = m.LastTicker;
                        trade.TickerMin = m.LastTicker;
                        trade.TickerMax = m.LastTicker;
                    }
                }
                catch (Exception ex)
                {
                    Global.Logger.Error($"UpdateOpenPositions {trade.Id} {ex.ToString()}");
                }

                new Thread(async () =>
                {
                    
                    await UpdateOpenOrder(trade);
                    try
                    {
                        await Global.DataStore.SaveTradeAsync(trade);
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.Error($"SaveTrade {trade.Id} {trade.PerformanceHistory?.Length} {ex.ToString()}");
                        trade.PerformanceHistory = new decimal[0];

                        try
                        {
                            await Global.DataStore.SaveTradeAsync(trade);
                            Global.Logger.Error($"SaveTrade Second attempt successfull {trade.Id} {trade.PerformanceHistory?.Length} {ex.ToString()}");
                        }
                        catch (Exception ex2)
                        {
                            Global.Logger.Error($"SaveTrade Second attempt fail {trade.Id} {trade.PerformanceHistory?.Length} {ex2.ToString()}");
                            trade.PerformanceHistory = new decimal[0];

                        }




                    }
                }).Start();
            }

            DepotManager.RiskValue = riskcapital;

        }

        /// <summary>
        /// Checks the implemented trading indicator(s),
        /// if one pair triggers the buy signal a new trade record gets created.
        /// </summary>
        /// <returns></returns>
        private async void FindOpportunities()
        {
            foreach (var m in MarketManager.Markets.Values.Where(m => m.Active))
            {
                var (advice, trade) = m.GetBuyAdvice(trades);

                if (advice.Advice == TradeAdviceEnum.Sell) // && !DepotManager.HasPosition(m.CurrencyPair.BaseCurrency) && DepotManager.HasPosition(m.SettleCurrency))
                {
                    Global.Logger.Information($"Signal {advice.Advice} {m.GlobalMarketName} {advice.Strategy} ({advice.Comment})");
                    advice.Advice = TradeAdviceEnum.Buy;
                    advice.Action = TradeActionEnum.Short;
                    if (await ExecuteTrade(m, trade, advice))
                    {
                        // immer nur max ein trade pro cycle
                        return;
                    }
                }
                else
                if (advice.Advice == TradeAdviceEnum.Buy) // && !DepotManager.HasPosition(m.CurrencyPair.BaseCurrency) && DepotManager.HasPosition(m.SettleCurrency))
                {
                    Global.Logger.Information($"Signal {advice.Advice} {m.GlobalMarketName} {advice.Strategy} ({advice.Comment})");
                    advice.Advice = TradeAdviceEnum.Buy;
                    advice.Action = TradeActionEnum.Long;
                    if (await ExecuteTrade(m, trade, advice))
                    {
                        // immer nur max ein trade pro cycle
                        return;
                    }
                }
            }
        }


        private async Task<bool> ExecuteTrade(TradeMarket Market, Trade trade, TradeAdvice advice)
        {
            var side = (advice.Advice == TradeAdviceEnum.Buy) ? OrderSide.Open : OrderSide.Close;
            var actionOpen = advice.Action;
            var amountQuoteCurrency = 0m;
            var amountBaseCurrency = 0m;
            var quantity = 0m;
            string orderId = null;

            // buy
            if (side == OrderSide.Open)
            {
                var currency = Market.SettleCurrency;
                var fee = 0.0005m;

                var amountSettleCurency = DepotManager.GetPositionSize(currency, fee);

                if (Market.CurrencyPair.QuoteCurrency == "USD" || Market.CurrencyPair.QuoteCurrency == "USDT")
                    amountQuoteCurrency = MarketManager.GetUSD(currency, amountSettleCurency);
                if (Market.CurrencyPair.QuoteCurrency == "BTC")
                    amountQuoteCurrency = MarketManager.GetBTC(currency, amountSettleCurency);

                amountBaseCurrency = amountQuoteCurrency / Market.Last.Close;
                
                //if (trade != null && amountBaseCurrency > 0)
                //{
                //    var currentBaseCurrency = trade.Quantity / Market.QuoteToSettle;
                //    if (currentBaseCurrency / amountBaseCurrency >= 3.0m)
                //    {
                //        Global.Logger.Information($"DCABuy Stop {Market.GlobalMarketName} {currentBaseCurrency:N2} {amountQuoteCurrency:N2} {amountBaseCurrency:N2}");
                //        // no DCA if very big position
                //        amountQuoteCurrency = 0;
                //        amountBaseCurrency = 0;
                //    }
                //    else 
                //    {
                //        // DCA only x% PositionSize
                //        amountQuoteCurrency *= DepotManager.mDCAPosPercent;
                //        amountBaseCurrency = amountQuoteCurrency / Market.Last.Close;
                //        Global.Logger.Information($"DCABuy Limit {Market.GlobalMarketName} {currentBaseCurrency:N2} {amountQuoteCurrency:N2} {amountBaseCurrency:N2}");
                //    }
                //}

                quantity = Math.Floor(amountBaseCurrency * Market.QuoteToSettle / Market.LotSize) * Market.LotSize;
                if (quantity == 0) return false;

                if (Global.Configuration.TradeOptions.PaperTrade)
                    orderId = "PaperTrade-" + Guid.NewGuid().ToString().Replace("-", "");
                else if (actionOpen == TradeActionEnum.Long)
                    orderId = Global.ExchangeApi.Buy(Market.GlobalMarketName, quantity, Market.Last.Close).Result;
                else
                    orderId = Global.ExchangeApi.Sell(Market.GlobalMarketName, quantity, Market.Last.Close).Result;
            }

            // sell
            if (side == OrderSide.Close && trade != null)
            {
                var amountToOrder = trade.Quantity;
                amountBaseCurrency = amountToOrder / Market.QuoteToSettle;
                amountQuoteCurrency = amountBaseCurrency * Market.Last.Close; // balance is in QuoteCurrency

                if (amountToOrder == 0) return false;

                if (trade.IsPaperTrading)
                    orderId = "PaperTrade-" + Guid.NewGuid().ToString().Replace("-", "");
                else if (trade.PositionType == PositionType.Long)
                    orderId = Global.ExchangeApi.Sell(Market.GlobalMarketName, amountToOrder, Market.Last.Close).Result;
                else
                    orderId = Global.ExchangeApi.Buy(Market.GlobalMarketName, amountToOrder, Market.Last.Close).Result;
            }

            var arrow = (side == OrderSide.Open) ? "<<" : ">>";
            Global.Logger.Information($"Order {side} {Market.GlobalMarketName} @ {Market.Last.Close} {amountBaseCurrency}{Market.CurrencyPair.BaseCurrency} {arrow} {amountQuoteCurrency}{Market.CurrencyPair.QuoteCurrency} Signal {advice.Comment}");

            if (orderId == null)
            {
                Global.Logger.Error($"Error Order {side} {Market.GlobalMarketName}, terminate");
                return true;
            }

            var stake = MarketManager.GetUSD(Market.CurrencyPair.BaseCurrency, amountBaseCurrency);

            if (side == OrderSide.Open)
            {
                if (trade == null)
                {
                    trade = new Trade();
                    trade.GlobalSymbol = Market.GlobalMarketName;
                    trade.Market = Market.GlobalMarketName;
                    trade.Exchange = Market.Exchange;
                    trade.StrategyUsed = advice.Strategy;

                    trade.StakeAmount = stake;

                    trade.BuyType = BuyType.Strategy;
                    trade.SellOnPercentage = 0.0m;
                    trade.OpenDate = DateTime.UtcNow;
                    trade.PositionType = (actionOpen == TradeActionEnum.Long) ? PositionType.Long: PositionType.Short;

                    if (orderId.StartsWith("PaperTrade"))
                    {
                        trade.IsPaperTrading = true;
                        trade.Quantity = quantity; //save quantity as no real trade is executed
                    }
                    else
                    {
                        trade.IsPaperTrading = false;
                    }
                }
                else
                {
                    trade.StakeAmount += stake;
                    trade.BuyType = BuyType.Dca;
                    trade.DcaDate = DateTime.UtcNow;
                }

                trade.OpenOrderId = orderId;
                trade.BuyOrderId = orderId;
                trade.IsOpen = true;
                trade.IsBuying = true;

                trade.TickerLast = Market.LastTicker;

                Global.Logger.Information($"PosOpen {trade.GlobalSymbol} {trade.StrategyUsed} @ {trade.OpenDate.ToString("HH:mm:ss")} {Market.Last.Close}");
            }

            if (side == OrderSide.Close)
            {
                trade.CloseDate = DateTime.UtcNow;
                trade.CloseRate = Market.Last.Close;
                trade.OpenOrderId = orderId;
                trade.SellOrderId = orderId;
                trade.SellType = advice.SellType;
                trade.IsOpen = true;
                trade.IsSelling = true;

                trade.TickerLast = Market.LastTicker;

                Global.Logger.Information($"PosClose {advice.SellType} {trade.GlobalSymbol} @ {trade.OpenDate.ToString("HH:mm:ss")} {trade.CloseDate?.ToString("HH:mm:ss")} {trade.OpenRate} {trade.CloseRate}");
            }

            // Save the order.
            await Global.DataStore.SaveTradeAsync(trade);

            return true;
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

                Order exchangeOrder = null;

                if (trade.IsPaperTrading)
                {
                    exchangeOrder = new Order()
                    {
                        // fake order
                        Status = OrderStatus.Filled,
                        Price = trade.TickerLast.Mid(),
                        ExecutedQuantity = trade.Quantity,
                        OrderDate = DateTime.UtcNow
                    };
                    trade.Quantity = 0m; // used to save ordered quantity on paper
                }
                else
                {
                    exchangeOrder = await Global.ExchangeApi.GetOrder(trade.OpenOrderId, trade.Market);
                }

                if (exchangeOrder?.Status == OrderStatus.Filled)
                {
                    trade.OpenOrderId = null;

                    if(trade.IsBuying)
                    {
                        trade.IsBuying = false;

                        if (exchangeOrder.Price.HasValue && exchangeOrder.Price > 0)
                        {
                            var market = MarketManager.Markets[trade.GlobalSymbol];

                            if (trade.Quantity > 0)
                                // DCA, average price
                                trade.OpenRate = ((trade.Quantity / market.QuoteToSettle * trade.OpenRate) + (exchangeOrder.ExecutedQuantity / market.QuoteToSettle * exchangeOrder.Price.Value)) / ((trade.Quantity + exchangeOrder.ExecutedQuantity) / market.QuoteToSettle);
                            else
                            {
                                // first buy
                                trade.OpenRate = exchangeOrder.Price.Value;
                                trade.PerformanceHistory = null;
                                foreach (var t in market.LastTickers)
                                    trade.TickerLast = t; // add full History
                            }
                        }

                        trade.Quantity += exchangeOrder.ExecutedQuantity;

                         Global.Logger.Information($"Update Buy  {trade.GlobalSymbol} {trade.OpenDate.ToString("HH:mm:ss")}@{trade.OpenRate :N2}");
                    }

                    if (trade.IsSelling)
                    {
                        trade.IsOpen = false;
                        trade.IsSelling = false;

                        trade.CloseDate = exchangeOrder.OrderDate;
                        if (exchangeOrder.Price.HasValue && exchangeOrder.Price > 0) trade.CloseRate = exchangeOrder.Price.Value;

                        var market = MarketManager.Markets[trade.GlobalSymbol];
                        var stake = MarketManager.GetUSD(market.CurrencyPair.QuoteCurrency, trade.CloseRate.Value * trade.Quantity / market.QuoteToSettle);
                        trade.CloseProfit = stake - trade.StakeAmount;
                        if (trade.StakeAmount != 0m)
                            trade.CloseProfitPercentage = trade.CloseProfit / trade.StakeAmount * 100;

                        await Global.DataStore.SaveWalletTransactionAsync(new WalletTransaction()
                        {
                            Amount = trade.CloseProfit.Value,
                            Date = trade.CloseDate.Value
                        });

                        Global.Logger.Information($"Update Sell {trade.GlobalSymbol} {trade.OpenDate.ToString("HH:mm:ss")}@{trade.OpenRate :N2} {trade.CloseDate?.ToString("HH:mm:ss")}@{trade.CloseRate :N2} {trade.CloseProfitPercentage :N2}");
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

                    if (!trade.IsPaperTrading)
                        await Global.ExchangeApi.CancelOrder(trade.OpenOrderId, trade.Market);

                    Global.Logger.Information($"Order Buy canceled by timeout {trade.OpenOrderId}");

                    trade.OpenOrderId = null;
                    trade.IsOpen = false;
                    trade.IsBuying = false;
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
