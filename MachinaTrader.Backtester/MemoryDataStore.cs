using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Backtester
{
    public class MemoryDataStore : IDataStore
    {
        private ConcurrentDictionary<string, Trade> _trades = new ConcurrentDictionary<string, Trade>();
        private ConcurrentDictionary<Guid, WalletTransaction> _walletTransactions = new ConcurrentDictionary<Guid, WalletTransaction>();

        public MemoryDataStore()
        {
        }

        public async Task InitializeAsync()
        {
        }

        public async Task<List<Trade>> GetClosedTradesAsync(DateTime since)
        {
            var items = _trades.Values.Where(x => !x.IsOpen && x.CloseDate > since).ToList();
            return items;
        }

        public async Task<List<Trade>> GetActiveTradesAsync()
        {
            var items = _trades.Values.Where(x => x.IsOpen).ToList();
            return items;
        }

        public async Task<List<Trader>> GetAvailableTradersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<Trader>> GetBusyTradersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradeAsync(Trade trade)
        {
            _trades.TryRemove(trade.TradeId, out Trade removed);
            _trades.TryAdd(trade.TradeId, trade);
        }

        public async Task SaveWalletTransactionAsync(WalletTransaction walletTransaction)
        {
            _walletTransactions.TryRemove(walletTransaction.Id, out WalletTransaction removed);
            _walletTransactions.TryAdd(walletTransaction.Id, walletTransaction);
        }

        public async Task<List<WalletTransaction>> GetWalletTransactionsAsync()
        {
            var items = _walletTransactions.Values.OrderBy(s => s.Date).ToList();
            return items;
        }

        public async Task SaveTraderAsync(Trader trader)
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradersAsync(List<Trader> traders)
        {
            throw new NotImplementedException();
        }

        public async Task SaveTradesAsync(List<Trade> trades)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Trader>> GetTradersAsync()
        {
            throw new NotImplementedException();
        }
    }
}
