using Microsoft.Extensions.Logging;
using Mynt.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mynt.Core.TradeManagers
{
    public class BacktestTradeManager : ITradeManager
    {
        private readonly IExchangeApi _api;
        private readonly INotificationManager _notification;
        private readonly ITradingStrategy _strategy;
        private readonly ILogger _logger;
        private List<Trade> _activeTrades;
        private List<Trader> _currentTraders;
        private readonly IDataStore _dataStore;
        private readonly OrderBehavior _orderBehavior;
        private readonly TradeOptions _settings;

        public BacktestTradeManager(IExchangeApi api,
                                    ITradingStrategy strategy,
                                    INotificationManager notificationManager,
                                    ILogger logger,
                                    TradeOptions settings,
                                    IDataStore dataStore,
                                    OrderBehavior orderBehavior = OrderBehavior.AlwaysFill)
        {
            _api = api;
            _strategy = strategy;
            _logger = logger;
            _notification = notificationManager;
            _dataStore = dataStore;
            _orderBehavior = orderBehavior;
            _settings = settings;

            if (_api == null) throw new ArgumentException("Invalid exchange provided...");
            if (_strategy == null) throw new ArgumentException("Invalid strategy provided...");
            if (_dataStore == null) throw new ArgumentException("Invalid data store provided...");
            if (_settings == null) throw new ArgumentException("Invalid settings provided...");
            if (_logger == null) throw new ArgumentException("Invalid logger provided...");
        }

        public Task LookForNewTrades()
        {
            throw new NotImplementedException();
        }

        public Task UpdateExistingTrades()
        {
            throw new NotImplementedException();
        }
    }
}
