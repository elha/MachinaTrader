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
    public class ExchangeBacktestGdaxAPI : ExchangeAPI
    {
        public override string BaseUrl { get => "local"; set => throw new NotImplementedException(); }

        public override string Name => "ExchangeBacktestGdaxAPI";

        public async Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> GetTickersAsync()
        {
            var tickers = new List<KeyValuePair<string, ExchangeTicker>>();
            var listOfMakert = new List<string>();
            foreach (var market in listOfMakert)
            {
                string symbol = "".ToStringInvariant();
                var ticker = new ExchangeTicker()
                {

                };

                tickers.Add(new KeyValuePair<string, ExchangeTicker>(symbol, ticker));
            }
            return tickers;
        }

        public async Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(string symbol = null)
        {
            var orders = new List<ExchangeOrderResult>();

            var o = new ExchangeOrderResult()
            {

            };

            return orders;
        }

        public async Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, string symbol = null)
        {
            var result = new ExchangeOrderResult();

            return result;
        }

        public async Task<ExchangeOrderBook> GetOrderBookAsync(string symbol, int maxCount = 100)
        {
            var result = new ExchangeOrderBook();

            return result;
        }

        public async Task<ExchangeTicker> GetTickerAsync(string symbol)
        {
            var result = new ExchangeTicker() { };
            return result;
        }

        public async Task<IEnumerable<MarketCandle>> GetCandlesAsync(string symbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
           var candles = new List<MarketCandle>();

            var obj = new List<string>();

            //foreach (object array in obj)
            //{
            //    candles.Add(new MarketCandle
            //    {
            //        ClosePrice = array[4].ConvertInvariant<decimal>(),
            //        ExchangeName = Name,
            //        HighPrice = array[2].ConvertInvariant<decimal>(),
            //        LowPrice = array[3].ConvertInvariant<decimal>(),
            //        Name = symbol,
            //        OpenPrice = array[1].ConvertInvariant<decimal>(),
            //        PeriodSeconds = periodSeconds,
            //        Timestamp = CryptoUtility.UnixTimeStampToDateTimeMilliseconds(array[0].ConvertInvariant<long>()),
            //        BaseVolume = array[5].ConvertInvariant<double>(),
            //        ConvertedVolume = array[7].ConvertInvariant<double>(),
            //        WeightedAverage = 0m
            //    });
            //}

            return candles;
        }

        public async Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order)
        {
            var result = new ExchangeOrderResult() { };
            return result;
        }

        public async Task<IEnumerable<ExchangeMarket>> GetSymbolsMetadataAsync()
        {
            var markets = new List<ExchangeMarket>();            

            return markets;
        }

        public virtual string GlobalSymbolToExchangeSymbol(string symbol)
        {
            return string.Empty;
        }

        public virtual string ExchangeSymbolToGlobalSymbol(string symbol)
        {
            return string.Empty;
        }
    }
}
