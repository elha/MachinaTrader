using MongoDB.Driver;
using Mynt.Core.Interfaces;
using Mynt.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mynt.Data.MongoDB
{
    public class MongoDbDataStore : IDataStore
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        public static MongoDbOptions MongoDbOptions;
        private IMongoCollection<TraderAdapter> _traderAdapter;
        private IMongoCollection<TradeAdapter> _ordersAdapter;

        public MongoDbDataStore(MongoDbOptions options)
        {
            MongoDbOptions = options;
            _client = new MongoClient(options.MongoUrl);
            _database = _client.GetDatabase("Mynt");
            _ordersAdapter = _database.GetCollection<TradeAdapter>("Orders");
            _traderAdapter = _database.GetCollection<TraderAdapter>("Traders");
        }

        public async Task InitializeAsync()
        {
        }

        public async Task<List<Trade>> GetClosedTradesAsync()
        {
            var trades = await _ordersAdapter.Find(x => !x.IsOpen).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trade>>(trades);

            return items;
        }

        public async Task<List<Trade>> GetActiveTradesAsync()
        {
            var trades = await _ordersAdapter.Find(x => x.IsOpen).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trade>>(trades);

            return items;
        }

        public async Task<List<Trader>> GetAvailableTradersAsync()
        {
            var traders = await _traderAdapter.Find(x => !x.IsBusy && !x.IsArchived).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

        public async Task<List<Trader>> GetBusyTradersAsync()
        {
            var traders = await _traderAdapter.Find(x => x.IsBusy && !x.IsArchived).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

        public async Task SaveTradeAsync(Trade trade)
        {
            var item = Mapping.Mapper.Map<TradeAdapter>(trade);
            TradeAdapter checkExist = await _ordersAdapter.Find(x => x.TradeId.Equals(item.TradeId)).FirstOrDefaultAsync();
            if (checkExist != null)
            {
                await _ordersAdapter.ReplaceOneAsync(x => x.TradeId.Equals(item.TradeId), item);
            } else
            {
                await _ordersAdapter.InsertOneAsync(item);
            }
        }

        public async Task SaveTraderAsync(Trader trader)
        {
            var item = Mapping.Mapper.Map<TraderAdapter>(trader);
            TraderAdapter checkExist = await _traderAdapter.Find(x => x.Identifier.Equals(item.Identifier)).FirstOrDefaultAsync();
            if (checkExist != null)
            {
                await _traderAdapter.ReplaceOneAsync(x => x.Identifier.Equals(item.Identifier), item);
            }
            else
            {
                await _traderAdapter.InsertOneAsync(item);
            }
        }

        public async Task SaveTradersAsync(List<Trader> traders)
        {
            var items = Mapping.Mapper.Map<List<TraderAdapter>>(traders);

            foreach (var item in items)
            {
                TraderAdapter checkExist = await _traderAdapter.Find(x => x.Identifier.Equals(item.Identifier)).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    await _traderAdapter.ReplaceOneAsync(x => x.Identifier.Equals(item.Identifier), item);
                }
                else
                {
                    await _traderAdapter.InsertOneAsync(item);
                }
            }
        }

        public async Task SaveTradesAsync(List<Trade> trades)
        {
            var items = Mapping.Mapper.Map<List<TradeAdapter>>(trades);

            foreach (var item in items)
            {
                TradeAdapter checkExist = await _ordersAdapter.Find(x => x.TradeId.Equals(item.TradeId)).FirstOrDefaultAsync();
                if (checkExist != null)
                {
                    await _ordersAdapter.ReplaceOneAsync(x => x.TradeId.Equals(item.TradeId), item);
                }
                else
                {
                    await _ordersAdapter.InsertOneAsync(item);
                }
            }
        }

        public async Task<List<Trader>> GetTradersAsync()
        {
            var traders = await _traderAdapter.Find(_ => true).ToListAsync();
            var items = Mapping.Mapper.Map<List<Trader>>(traders);

            return items;
        }

    }
}
