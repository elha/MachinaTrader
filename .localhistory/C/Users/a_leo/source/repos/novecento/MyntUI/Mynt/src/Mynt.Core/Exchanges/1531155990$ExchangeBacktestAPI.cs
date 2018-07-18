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

        public Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, string symbol = null)
        {
            Dictionary<string, object> payload = GetNoncePayload();
            if (string.IsNullOrEmpty(symbol))
            {
                throw new InvalidOperationException("Binance single order details request requires symbol");
            }
            symbol = NormalizeSymbol(symbol);
            payload["symbol"] = symbol;
            payload["orderId"] = orderId;
            JToken token = await MakeJsonRequestAsync<JToken>("/order", BaseUrlPrivate, payload);
            CheckError(token);
            ExchangeOrderResult result = ParseOrder(token);

            // Add up the fees from each trade in the order
            Dictionary<string, object> feesPayload = GetNoncePayload();
            feesPayload["symbol"] = symbol;
            JToken feesToken = await MakeJsonRequestAsync<JToken>("/myTrades", BaseUrlPrivate, feesPayload);
            CheckError(feesToken);
            ParseFees(feesToken, result);

            return result;
        }

        public Task<ExchangeOrderBook> GetOrderBookAsync(string symbol, int maxCount = 100)
        {
            symbol = NormalizeSymbol(symbol);
            JToken obj = await MakeJsonRequestAsync<JToken>("/depth?symbol=" + symbol + "&limit=" + maxCount);
            CheckError(obj);
            return ParseOrderBook(obj);
        }

        public Task<ExchangeTicker> GetTickerAsync(string symbol)
        {
            symbol = NormalizeSymbol(symbol);
            JToken obj = await MakeJsonRequestAsync<JToken>("/ticker/24hr?symbol=" + symbol);
            CheckError(obj);
            return ParseTicker(symbol, obj)
        }

        public Task<IEnumerable<MarketCandle>> GetCandlesAsync(string symbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
            List<MarketCandle> candles = new List<MarketCandle>();
            symbol = NormalizeSymbol(symbol);
            string url = "/klines?symbol=" + symbol;
            if (startDate != null)
            {
                url += "&startTime=" + (long)startDate.Value.UnixTimestampFromDateTimeMilliseconds();
                url += "&endTime=" + ((endDate == null ? long.MaxValue : (long)endDate.Value.UnixTimestampFromDateTimeMilliseconds())).ToStringInvariant();
            }
            if (limit != null)
            {
                url += "&limit=" + (limit.Value.ToStringInvariant());
            }
            string periodString = CryptoUtility.SecondsToPeriodString(periodSeconds);
            url += "&interval=" + periodString;
            JToken obj = await MakeJsonRequestAsync<JToken>(url);
            CheckError(obj);
            foreach (JArray array in obj)
            {
                candles.Add(new MarketCandle
                {
                    ClosePrice = array[4].ConvertInvariant<decimal>(),
                    ExchangeName = Name,
                    HighPrice = array[2].ConvertInvariant<decimal>(),
                    LowPrice = array[3].ConvertInvariant<decimal>(),
                    Name = symbol,
                    OpenPrice = array[1].ConvertInvariant<decimal>(),
                    PeriodSeconds = periodSeconds,
                    Timestamp = CryptoUtility.UnixTimeStampToDateTimeMilliseconds(array[0].ConvertInvariant<long>()),
                    BaseVolume = array[5].ConvertInvariant<double>(),
                    ConvertedVolume = array[7].ConvertInvariant<double>(),
                    WeightedAverage = 0m
                });
            }

            return candles;
        }

        public Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order)
        {
            string symbol = NormalizeSymbol(order.Symbol);
            Dictionary<string, object> payload = GetNoncePayload();
            payload["symbol"] = symbol;
            payload["side"] = order.IsBuy ? "BUY" : "SELL";
            payload["type"] = order.OrderType.ToStringUpperInvariant();

            // Binance has strict rules on which prices and quantities are allowed. They have to match the rules defined in the market definition.
            decimal outputQuantity = ClampOrderQuantity(symbol, order.Amount);
            decimal outputPrice = ClampOrderPrice(symbol, order.Price);

            payload["quantity"] = outputQuantity;
            payload["newOrderRespType"] = "FULL";

            if (order.OrderType != OrderType.Market)
            {
                payload["timeInForce"] = "GTC";
                payload["price"] = outputPrice;
            }
            foreach (var kv in order.ExtraParameters)
            {
                payload[kv.Key] = kv.Value;
            }

            JToken token = await MakeJsonRequestAsync<JToken>("/order", BaseUrlPrivate, payload, "POST");
            CheckError(token);
            return ParseOrder(token);
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
