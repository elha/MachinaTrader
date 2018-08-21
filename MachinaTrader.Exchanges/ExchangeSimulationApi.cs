using ExchangeSharp;
using MachinaTrader.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Exchanges
{
    public class ExchangeSimulationApi : ExchangeAPI
    {
        public override string BaseUrl { get => "local"; set => throw new NotImplementedException(); }

        public override string Name => _realApi.Name;

        private readonly ExchangeAPI _realApi;

        private Dictionary<DateTime, decimal> _wallet = new Dictionary<DateTime, decimal>();
        private List<ExchangeOrderResult> _orders = new List<ExchangeOrderResult>();

        public ExchangeSimulationApi(ExchangeAPI realApi)
        {
            _realApi = realApi;

#warning TODO: get from configuration
            _wallet.Add(DateTime.UtcNow, 500);
        }

        protected override async Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> OnGetTickersAsync()
        {
            var tickers = new List<KeyValuePair<string, ExchangeTicker>>();

            //get markets of exchange by base currency
            var listOfMakert = new List<string>();

            var exchangeCoins = this.GetSymbolsMetadataAsync().Result.Where(m => m.BaseCurrency == Global.Configuration.TradeOptions.QuoteCurrency);
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

        protected override async Task<ExchangeTicker> OnGetTickerAsync(string symbol)
        {
            return GetExchangeTicker(symbol);
        }

        protected override async Task<IEnumerable<MarketCandle>> OnGetCandlesAsync(string symbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
            //Global.Logger.Information($"Starting OnGetCandlesAsync {symbol}");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var candles = new List<MarketCandle>();

            var cachedCandles = Global.AppCache.Get<List<Candle>>(_realApi.Name + symbol + Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationCandleSize);
            if (cachedCandles == null)
                return null;

            var items = cachedCandles.Where(c => c.Timestamp > startDate.Value && c.Timestamp <= endDate.Value).ToList();

            foreach (Candle item in items)
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

            //watch1.Stop();
            //Global.Logger.Warning($"Ended OnGetCandlesAsync {symbol} in #{watch1.Elapsed.TotalSeconds} seconds");

            return candles;
        }

        protected override async Task<IEnumerable<ExchangeMarket>> OnGetSymbolsMetadataAsync()
        {
            //Global.Logger.Information($"Starting OnGetSymbolsMetadataAsync");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var markets = Global.AppCache.GetOrAdd(_realApi.Name, async (a) => await _realApi.GetSymbolsMetadataAsync());
            if (markets.Result.Count() == 0)
                throw new Exception();

            //watch1.Stop();
            //Global.Logger.Warning($"Ended OnGetSymbolsMetadataAsync in #{watch1.Elapsed.TotalSeconds} seconds");

            return markets.Result;
        }

        protected override async Task<IEnumerable<string>> OnGetSymbolsAsync()
        {
            return (await GetSymbolsMetadataAsync()).Select(market => market.MarketName);
        }

        private ExchangeTicker GetExchangeTicker(string symbol)
        {
            //Global.Logger.Information($"Starting GetExchangeTicker {symbol}");
            //var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var candles = Global.AppCache.Get<List<Candle>>(_realApi.Name + symbol + "1");
            if (candles == null)
                return null;

            candles = candles.Where(c => c.Timestamp <= Global.Configuration.ExchangeOptions.FirstOrDefault().SimulationCurrentDate).ToList();

            var lastCandle = candles.Last();

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

            //watch1.Stop();
            //Global.Logger.Warning($"Ended GetExchangeTicker {symbol} in #{watch1.Elapsed.TotalSeconds} seconds");

            return ticker;
        }

        public override string ExchangeSymbolToGlobalSymbol(string symbol)
        {
            string[] pieces = symbol.Split('-');
            return pieces.First() + pieces.Last();
        }

     
        protected override async Task<ExchangeOrderResult> OnGetOrderDetailsAsync(string orderId, string symbol = null)
        {
            return _orders.Where(o => o.OrderId == orderId).FirstOrDefault();
        }

        protected override async Task OnCancelOrderAsync(string orderId, string symbol = null)
        {
            _orders.RemoveAll(o => o.OrderId == orderId);
        }

        protected override async Task<ExchangeOrderResult> OnPlaceOrderAsync(ExchangeOrderRequest order)
        {
            if (order.IsBuy)
            {
                _wallet.Add(DateTime.UtcNow, -(order.Price * order.Amount));
            }
            else
            {
                _wallet.Add(DateTime.UtcNow, order.Price * order.Amount);
            }

            var orderResult = new ExchangeOrderResult()
            {
                OrderId = Guid.NewGuid().ToString().Replace("-", string.Empty),
                Result = ExchangeAPIOrderResult.Filled,
                Message = "",
                Amount = order.Amount,
                AmountFilled = order.Amount,
                Price = order.Price,
                AveragePrice = order.Price,
                OrderDate = DateTime.UtcNow,
                Symbol = order.Symbol,
                IsBuy = order.IsBuy,
                Fees = 0m,
                FeesCurrency = ""
            };

            _orders.Add(orderResult);

            return orderResult;
        }

        protected override async Task<Dictionary<string, decimal>> OnGetAmountsAvailableToTradeAsync()
        {
            var balances = new Dictionary<string, decimal>();

            decimal balance = 0m;
            foreach (var item in _wallet)
            {
                balance = balance + item.Value;
            }

            balances.Add(Global.Configuration.TradeOptions.QuoteCurrency, balance);
            return balances;
        }
    }
}
