using ExchangeSharp;
using Mynt.Core.Enums;
using Mynt.Core.Interfaces;
using Mynt.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mynt.Core.Exchanges
{
    public class ExchangeBacktestGdaxAPI : ExchangeAPI, IExchangeApi
    {
        public override string BaseUrl { get => "local"; set => throw new NotImplementedException(); }

        public override string Name => "ExchangeBacktestGdaxAPI";

        public Task<string> Buy(string market, decimal quantity, decimal rate)
        {
            throw new NotImplementedException();
        }

        public Task<AccountBalance> GetBalance(string currency)
        {
            throw new NotImplementedException();
        }

        public Task<List<Models.MarketSummary>> GetMarketSummaries(string quoteCurrency)
        {
            throw new NotImplementedException();
        }

        public Task<List<OpenOrder>> GetOpenOrders(string market)
        {
            throw new NotImplementedException();
        }

        public Task<Order> GetOrder(string orderId, string market)
        {
            throw new NotImplementedException();
        }

        public Task<OrderBook> GetOrderBook(string market)
        {
            throw new NotImplementedException();
        }

        public Task<List<Candle>> GetTickerHistory(string market, Period period, DateTime startDate, DateTime? endDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<Candle>> GetTickerHistory(string market, Period period, int length)
        {
            throw new NotImplementedException();
        }

        public Task<string> Sell(string market, decimal quantity, decimal rate)
        {
            throw new NotImplementedException();
        }

        Task IExchangeApi.CancelOrder(string orderId, string market)
        {
            throw new NotImplementedException();
        }

        Task<Ticker> IExchangeApi.GetTicker(string market)
        {
            throw new NotImplementedException();
        }
    }
}
