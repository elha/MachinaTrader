using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mynt.Core.Interfaces;
using Mynt.Core.Models;

namespace Mynt.Core.Backtester
{
    public class DatabaseCandleProvider
    {
        public async Task<List<Candle>> GetCandles(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            List<Candle> candles = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);

            return candles;
        }

        public async Task SaveTradeSignals(BacktestOptions backtestOptions, IDataStoreBacktest dataStore, List<TradeSignal> signals)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            await dataStore.SaveBacktestTradeSignalsBulk(signals, backtestOptions);
        }

        public async Task<List<TradeSignal>> GetSignals(BacktestOptions backtestOptions, IDataStoreBacktest dataStore, string strategy)
        {
            if (backtestOptions.EndDate == DateTime.MinValue)
            {
                backtestOptions.EndDate = DateTime.UtcNow;
            }

            List<TradeSignal> items = await dataStore.GetBacktestSignalsByStrategy(backtestOptions, strategy);

            return items;
        }
    }
}
