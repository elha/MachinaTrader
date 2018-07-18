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

        public Task<IEnumerable<ExchangeMarket>> GetSymbolsMetadataAsync()
        {
            var markets = new List<ExchangeMarket>();
            JToken obj = await MakeJsonRequestAsync<JToken>("/exchangeInfo");
            CheckError(obj);
            JToken allSymbols = obj["symbols"];
            foreach (JToken symbol in allSymbols)
            {
                var market = new ExchangeMarket
                {
                    MarketName = symbol["symbol"].ToStringUpperInvariant(),
                    IsActive = ParseMarketStatus(symbol["status"].ToStringUpperInvariant()),
                    BaseCurrency = symbol["quoteAsset"].ToStringUpperInvariant(),
                    MarketCurrency = symbol["baseAsset"].ToStringUpperInvariant()
                };

                // "LOT_SIZE"
                JToken filters = symbol["filters"];
                JToken lotSizeFilter = filters?.FirstOrDefault(x => string.Equals(x["filterType"].ToStringUpperInvariant(), "LOT_SIZE"));
                if (lotSizeFilter != null)
                {
                    market.MaxTradeSize = lotSizeFilter["maxQty"].ConvertInvariant<decimal>();
                    market.MinTradeSize = lotSizeFilter["minQty"].ConvertInvariant<decimal>();
                    market.QuantityStepSize = lotSizeFilter["stepSize"].ConvertInvariant<decimal>();
                }

                // PRICE_FILTER
                JToken priceFilter = filters?.FirstOrDefault(x => string.Equals(x["filterType"].ToStringUpperInvariant(), "PRICE_FILTER"));
                if (priceFilter != null)
                {
                    market.MaxPrice = priceFilter["maxPrice"].ConvertInvariant<decimal>();
                    market.MinPrice = priceFilter["minPrice"].ConvertInvariant<decimal>();
                    market.PriceStepSize = priceFilter["tickSize"].ConvertInvariant<decimal>();
                }
                markets.Add(market);
            }

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
