using MachinaTrader.Globals.Structure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeSharp;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Enums;
using Microsoft.Extensions.Caching.Memory;
using MachinaTrader.Globals.Structure.Extensions;

namespace MachinaTrader.Backtester
{
    public class DatabaseCandleProvider
    {
        public List<Candle> GetCandles(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            lock (dataStore)
            {
                return  dataStore.GetBacktestCandlesBetweenTime(backtestOptions).Result;
            }
        }

       
        public async Task<Tuple<DateTime,DateTime>> CacheAllData(ExchangeAPI api, Exchange exchange)
        {
            Global.Logger.Information($"Starting CacheAllData");
            var watch1 = System.Diagnostics.Stopwatch.StartNew();

            var Symbols = Global.Configuration.TradeOptions.TradeAssetsList().ToList();
            Symbols.Add(Global.Configuration.TradeOptions.QuoteCurrency);

            // If there are items on the only trade list remove the rest
            var exchangeCoins = api.GetMarketSymbolsMetadataAsync().Result.Where(m =>
                Symbols.Any(c => c == m.BaseCurrency) && Symbols.Any(c => c == m.QuoteCurrency)
            ).ToList();

            var currentExchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();

            IExchangeAPI realExchange = ExchangeAPI.GetExchangeAPI(api.Name);

            var returns = new Tuple<DateTime, DateTime>(DateTime.MinValue,DateTime.MinValue);

            foreach (var coin in exchangeCoins)
            {
                var symbol = coin.MarketSymbol;

                if (realExchange is ExchangeBinanceAPI)
                    symbol = await api.ExchangeMarketSymbolToGlobalMarketSymbolAsync(symbol);

                var backtestOptions = new BacktestOptions
                {
                    DataFolder = Global.DataPath,
                    Exchange = exchange,
                    Coin = symbol,
                    CandlePeriod = Int32.Parse(currentExchangeOption.SimulationCandleSize)
                };

                var key1 = api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod;
                var data1 = Global.AppCache.Get<List<Candle>>(key1);
                if (data1 != null)
                {
                    returns = new Tuple<DateTime, DateTime>(data1.First().Timestamp, data1.Last().Timestamp);
                    continue;
                }

                Candle databaseFirstCandle = Global.DataStoreBacktest.GetBacktestFirstCandle(backtestOptions).Result;
                Candle databaseLastCandle = Global.DataStoreBacktest.GetBacktestLastCandle(backtestOptions).Result;

                if (databaseFirstCandle == null || databaseLastCandle == null)
                    continue;

                backtestOptions.StartDate = databaseFirstCandle.Timestamp;
                backtestOptions.EndDate = databaseLastCandle.Timestamp;

                var candleProvider = new DatabaseCandleProvider();
                var _candle15 = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);
                _candle15 = await _candle15.FillCandleGaps((Period)Enum.Parse(typeof(Period), backtestOptions.CandlePeriod.ToString(), true));

                Global.AppCache.Remove(backtestOptions.Coin + backtestOptions.CandlePeriod);
                Global.AppCache.Add(api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod, _candle15, new MemoryCacheEntryOptions());

                Global.Logger.Information($"   Cached {key1}");

                backtestOptions.CandlePeriod = 1;

                var key2 = api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod;
                if (Global.AppCache.Get<List<Candle>>(key2) != null)
                    continue;

                Candle database1FirstCandle = Global.DataStoreBacktest.GetBacktestFirstCandle(backtestOptions).Result;
                Candle database1LastCandle = Global.DataStoreBacktest.GetBacktestLastCandle(backtestOptions).Result;

                if (database1FirstCandle == null || database1LastCandle == null)
                    continue;

                backtestOptions.StartDate = database1FirstCandle.Timestamp;
                backtestOptions.EndDate = database1LastCandle.Timestamp;

                var _candle1 = candleProvider.GetCandles(backtestOptions, Global.DataStoreBacktest);
                _candle1 = await _candle1.FillCandleGaps((Period)Enum.Parse(typeof(Period), backtestOptions.CandlePeriod.ToString(), true));

                Global.AppCache.Remove(backtestOptions.Coin + backtestOptions.CandlePeriod);
                Global.AppCache.Add(api.Name + backtestOptions.Coin + backtestOptions.CandlePeriod, _candle1, new MemoryCacheEntryOptions());

                Global.Logger.Information($"   Cached {key2}");

                returns = new Tuple<DateTime, DateTime>(backtestOptions.StartDate, backtestOptions.EndDate);
            }

            watch1.Stop();
            Global.Logger.Warning($"Ended CacheAllData in #{watch1.Elapsed.TotalSeconds} seconds");

            return returns;
        }
    }
}
