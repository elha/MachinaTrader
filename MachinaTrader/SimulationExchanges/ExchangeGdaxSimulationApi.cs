using ExchangeSharp;
using MachinaTrader.Globals;
using Mynt.Core.Backtester;
using Mynt.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachinaTrader.SimulationExchanges
{
    public class ExchangeGdaxSimulationApi : ExchangeAPI
    {
        public override string BaseUrl { get => "local"; set => throw new NotImplementedException(); }

        public override string Name => "ExchangeGdaxSimulationApi";

        private readonly ExchangeAPI _realApi;

        public ExchangeGdaxSimulationApi()
        {
            _realApi = new ExchangeGdaxAPI();
        }

        protected override async Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> OnGetTickersAsync()
        {
            var tickers = new List<KeyValuePair<string, ExchangeTicker>>();

            //get markets of exchange by base currency
            var listOfMakert = new List<string>();

            var exchangeCoins = this.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == Runtime.Configuration.TradeOptions.QuoteCurrency);
            foreach (var coin in exchangeCoins)
            {
                listOfMakert.Add(this.ExchangeSymbolToGlobalSymbol(coin.MarketName));
            }

            foreach (var symbol in listOfMakert)
            {
                var ticker = GetExchangeTicker(symbol);
                if (ticker == null)
                    continue;

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
            var result = new ExchangeOrderResult()
            {
                OrderId = "",
                Result = ExchangeAPIOrderResult.Filled,
                Message = "",
                Amount = 0m,
                AmountFilled = 0m,
                Price = 0m,
                AveragePrice = 0m,
                OrderDate = DateTime.Now,
                Symbol = "XXXBTC",
                IsBuy = true,
                Fees = 0m,
                FeesCurrency = ""
            };

            return result;
        }

        public async Task<ExchangeOrderBook> GetOrderBookAsync(string symbol, int maxCount = 100)
        {
            var result = new ExchangeOrderBook()
            {

            };

            return result;
        }

        protected override async Task<ExchangeTicker> OnGetTickerAsync(string symbol)
        {
            return GetExchangeTicker(symbol);
        }

        protected override async Task<IEnumerable<MarketCandle>> OnGetCandlesAsync(string symbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
            var candles = new List<MarketCandle>();

            var backtestOptions = new BacktestOptions();
            backtestOptions.DataFolder = Global.DataPath;
            backtestOptions.Exchange = Exchange.Gdax;
            backtestOptions.Coin = symbol;
            backtestOptions.CandlePeriod = Int32.Parse(Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCandleSize);

            if (startDate.HasValue)
                backtestOptions.StartDate = startDate.Value;
            if (endDate.HasValue)
                backtestOptions.EndDate = endDate.Value;

            var candleProvider = new DatabaseCandleProvider();
            var items = await candleProvider.GetCandles(backtestOptions, Runtime.GlobalDataStoreBacktest);

            foreach (Mynt.Core.Models.Candle item in items)
            {
                try
                {
                    var marketCandle = new MarketCandle();
                    marketCandle.ClosePrice = item.Close.ConvertInvariant<decimal>();
                    marketCandle.ExchangeName = Name;
                    marketCandle.HighPrice = item.High.ConvertInvariant<decimal>();
                    marketCandle.LowPrice = item.Low.ConvertInvariant<decimal>();
                    marketCandle.Name = symbol;
                    marketCandle.OpenPrice = item.Open.ConvertInvariant<decimal>();
                    marketCandle.PeriodSeconds = periodSeconds;
                    marketCandle.Timestamp = item.Timestamp;
                    marketCandle.BaseVolume = item.Volume.ConvertInvariant<double>();
                    marketCandle.ConvertedVolume = (item.Volume * item.Close).ConvertInvariant<double>();
                    marketCandle.WeightedAverage = 0m;

                    candles.Add(marketCandle);
                }
                catch (Exception ex)
                {

                    throw;
                }
            }

            return candles;
        }

        public async Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order)
        {
            var result = new ExchangeOrderResult()
            {
                OrderId = "",
                Result = ExchangeAPIOrderResult.Filled,
                Message = "",
                Amount = 0m,
                AmountFilled = 0m,
                Price = 0m,
                AveragePrice = 0m,
                OrderDate = DateTime.Now,
                Symbol = "XXXBTC",
                IsBuy = true,
                Fees = 0m,
                FeesCurrency = "",
            };
            return result;
        }

        protected override async Task<IEnumerable<ExchangeMarket>> OnGetSymbolsMetadataAsync()
        {
            var markets = Global.AppCache.GetOrAdd("gdaxMarkets", async (a) => await _realApi.GetSymbolsMetadataAsync());
            if (markets.Result.Count() == 0)
                throw new Exception();

            return markets.Result;
        }

        private ExchangeMarket GetExchangeMarkets()
        {
            return new ExchangeMarket();
        }

        protected override async Task<IEnumerable<string>> OnGetSymbolsAsync()
        {
            return (await GetSymbolsMetadataAsync()).Select(market => market.MarketName);
        }

        private ExchangeTicker GetExchangeTicker(string symbol)
        {
            var backtestOptions = new BacktestOptions();
            backtestOptions.DataFolder = Global.DataPath;
            backtestOptions.Exchange = Exchange.Gdax;
            backtestOptions.Coin = symbol;
            backtestOptions.CandlePeriod = 1; //we need 1min database candles to best simulation of ticker
            backtestOptions.StartDate = Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate.AddMinutes(-30);
            backtestOptions.EndDate = Runtime.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate;

            var candleProvider = new DatabaseCandleProvider();
            var lastCandles = candleProvider.GetCandles(backtestOptions, Runtime.GlobalDataStoreBacktest).Result;
            var lastCandle = lastCandles.FirstOrDefault();

            if (lastCandle == null)
                return null;

            var ticker = new ExchangeTicker()
            {
                //weak assumptions with 1min database...
                Ask = lastCandle.Close,
                Last = lastCandle.Close,
                Bid = lastCandle.Close,
                Volume = new ExchangeVolume()
                {
                    BaseSymbol = symbol,
                    BaseVolume = lastCandle.Volume,
                    ConvertedSymbol = symbol,
                    ConvertedVolume = lastCandle.Volume * lastCandle.Close,
                    Timestamp = lastCandle.Timestamp
                }
            };

            return ticker;
        }

        public override string ExchangeSymbolToGlobalSymbol(string symbol)
        {
            string[] pieces = symbol.Split("-");

            return pieces.First() + pieces.Last();
        }
    }
}
