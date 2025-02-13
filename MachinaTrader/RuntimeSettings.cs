using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MachinaTrader.Helpers;
using MachinaTrader.Hubs;
using Quartz;
using Quartz.Impl;
using MachinaTrader.Globals.Structure.Enums;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using ExchangeSharp;
using MachinaTrader.Data.LiteDB;
using MachinaTrader.Data.MongoDB;
using MachinaTrader.Exchanges;
using MachinaTrader.Notifications;
using MachinaTrader.Backtester;

namespace MachinaTrader
{
    public static class Runtime
    {
        public static IHubContext<HubTraders> GlobalHubTraders;
        public static IHubContext<HubStatistics> GlobalHubStatistics;
        public static IHubContext<HubBacktest> GlobalHubBacktest;
        public static IHubContext<HubExchangeAccounts> GlobalHubAccounts;
        public static TelegramNotificationOptions GlobalTelegramNotificationOptions { get; set; }
        
        public static ConcurrentDictionary<string, Ticker> WebSocketTickers = new ConcurrentDictionary<string, Ticker>();

        public static List<string> GlobalCurrencys = new List<string>();
        public static List<string> ExchangeCurrencys = new List<string>();
    }

    /// <summary>
    /// Global Settings
    /// </summary>
    public class RuntimeSettings
    {
        public async static void Init()
        {
            Global.GlobalOrderBehavior = OrderBehavior.CheckMarket;

            Global.NotificationManagers = new List<INotificationManager>()
            {
                new SignalrNotificationManager(),
                new TelegramNotificationManager(Runtime.GlobalTelegramNotificationOptions)
            };

            if (Global.Configuration.SystemOptions.Database == "MongoDB")
            {
                Global.Logger.Information("Database set to MongoDB");
                MongoDbOptions databaseOptions = new MongoDbOptions();
                databaseOptions.MongoUrl = Global.DatabaseConnectionString;
                Global.DataStore = new MongoDbDataStore(databaseOptions);

                // Check DB connection
                MongoDbCheck(databaseOptions, databaseOptions.MongoDatabaseName);

                // Backtest MongoDB
                MongoDbOptions backtestDatabaseOptions = new MongoDbOptions();
                backtestDatabaseOptions.MongoUrl = Global.DatabaseConnectionString;
                Global.DataStoreBacktest = new MongoDbDataStoreBacktest(backtestDatabaseOptions);
            }
            else
            {
                Global.Logger.Information("Database set to LiteDB");
                LiteDbOptions databaseOptions = new LiteDbOptions { LiteDbName = Global.DataPath + "/MachinaTrader.db" };
                Global.DataStore = new LiteDbDataStore(databaseOptions);

                LiteDbOptions backtestDatabaseOptions = new LiteDbOptions { LiteDbName = Global.DataPath + "/MachinaTrader.db" };
                Global.DataStoreBacktest = new LiteDbDataStoreBacktest(backtestDatabaseOptions);
            }

            //we can set other Datastore in case of simulation
            var exchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();
            if (exchangeOption.IsSimulation)
                Global.DataStore = new MemoryDataStore();

            // Global Hubs
            Runtime.GlobalHubTraders = Global.ServiceScope.ServiceProvider.GetService<IHubContext<HubTraders>>();
            Runtime.GlobalHubStatistics = Global.ServiceScope.ServiceProvider.GetService<IHubContext<HubStatistics>>();
            Runtime.GlobalHubBacktest = Global.ServiceScope.ServiceProvider.GetService<IHubContext<HubBacktest>>();
            Runtime.GlobalHubAccounts = Global.ServiceScope.ServiceProvider.GetService<IHubContext<HubExchangeAccounts>>();

            //Run Cron
            IScheduler scheduler = Global.QuartzTimer;

            IJobDetail tradeTimerJob = JobBuilder.Create<Timers.TradeTimer>()
                .WithIdentity("tradeTimerJobTrigger", "tradeTimerJob")
                .Build();

            ITrigger tradeTimerJobTrigger;

            if (Global.Configuration.TradeOptions.TradeTimer != "")
            {
                tradeTimerJobTrigger = TriggerBuilder.Create()
                    .WithIdentity("tradeTimerJobTrigger", "tradeTimerJob")
                    .WithCronSchedule(Global.Configuration.TradeOptions.TradeTimer)
                    .UsingJobData("force", false)
                    .Build();

                await scheduler.ScheduleJob(tradeTimerJob, tradeTimerJobTrigger);
                Global.Logger.Information($"Trade Cron will run at: {tradeTimerJobTrigger.GetNextFireTimeUtc() ?? DateTime.MinValue:r}");
            }

            await scheduler.Start();
        }

        public static void LoadSettings()
        {
            var exchangeOption = Global.Configuration.ExchangeOptions.FirstOrDefault();
            switch (exchangeOption.Exchange)
            {
                case Exchange.CoinbaseSimulation:
                    exchangeOption.Exchange = Exchange.Coinbase;
                    Global.ExchangeApi = new BaseExchange(exchangeOption, new ExchangeSimulationApi((ExchangeAPI)ExchangeAPI.GetExchangeAPIAsync<ExchangeCoinbaseAPI>().Result));
                    Global.DataStore = new MemoryDataStore();
                    exchangeOption.IsSimulation = true;
                    break;
                case Exchange.BinanceSimulation:
                    exchangeOption.Exchange = Exchange.Binance;
                    Global.ExchangeApi = new BaseExchange(exchangeOption, new ExchangeSimulationApi((ExchangeAPI)ExchangeAPI.GetExchangeAPIAsync<ExchangeBinanceAPI>().Result));
                    Global.DataStore = new MemoryDataStore();
                    exchangeOption.IsSimulation = true;
                    break;
                default:
                    Global.ExchangeApi = new BaseExchange(exchangeOption);
                    exchangeOption.IsSimulation = false;
                    break;
            }

            //Websocket Test
            var fullApi = Global.ExchangeApi.GetFullApi();

            //Create Exchange Currencies as List
            foreach (var currency in Global.Configuration.TradeOptions.TradeAssetsList())
            {
                Runtime.GlobalCurrencys.Add(currency + "-" + Global.Configuration.TradeOptions.QuoteCurrency);
            }

            foreach (var currency in Runtime.GlobalCurrencys)
            {
                try
                {
                    Runtime.ExchangeCurrencys.Add(fullApi.GlobalMarketSymbolToExchangeMarketSymbolAsync(currency).Result);
                }
                catch (Exception ex) { }
             }

            if (!exchangeOption.IsSimulation)
            {
                //fullApi.GetTickersWebSocketAsync(OnWebsocketTickersUpdated);
                //fullApi.GetTradesWebSocketAsync(OnWebsocketTickersUpdated);
                //fullApi.GetCompletedOrderDetailsWebSocketAsync(OnWebsocketTickersUpdated);
                //fullApi.GetDeltaOrderBookWebSocketAsync(OnWebsocketTickersUpdated);
                //fullApi.GetOrderDetailsWebSocketAsync(OnWebsocketTickersUpdated);
                //fullApi.GetUserDataWebSocketAsync(OnWebsocketTickersUpdated,"");
            }
            // Telegram Notifications
            Runtime.GlobalTelegramNotificationOptions = Global.Configuration.TelegramOptions;
        }

        public static void OnWebsocketTickersUpdated(IReadOnlyCollection<KeyValuePair<string, ExchangeSharp.ExchangeTicker>> updatedTickers)
        {
            foreach (var update in updatedTickers)
            {
                if (Runtime.ExchangeCurrencys.Contains(update.Key))
                {
                    if (Runtime.WebSocketTickers.TryGetValue(update.Key, out Ticker ticker))
                    {
                        ticker.Ask = update.Value.Ask;
                        ticker.Bid = update.Value.Bid;
                        ticker.Last = update.Value.Last;
                    }
                    else
                    {
                        Runtime.WebSocketTickers.TryAdd(update.Key, new Ticker
                        {
                            Ask = update.Value.Ask,
                            Bid = update.Value.Bid,
                            Last = update.Value.Last
                        });
                    }
                }
            }
        }

        public static void MongoDbCheck(MongoDbOptions databaseOptions, string dbName)
        {
            var client = new MongoClient(databaseOptions.MongoUrl);
            var database = client.GetDatabase(dbName);
            var isMongoLive = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);

            while (!isMongoLive)
            {
                Global.Logger.Error("MongoDB: Connection to {0} FAILED! Waiting for connection", dbName);
                Thread.Sleep(1000);
                isMongoLive = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }

            Global.Logger.Information("MongoDB: Connection to {0} SUCCESSFUL!", dbName);
        }
    }
}
