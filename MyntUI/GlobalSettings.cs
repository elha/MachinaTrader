using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mynt.Core.Enums;
using Mynt.Core.Exchanges;
using Mynt.Core.Interfaces;
using Mynt.Core.Models;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using Mynt.Data.LiteDB;
using Mynt.Data.MongoDB;
using MachinaTrader.Helpers;
using MachinaTrader.Hubs;
using MachinaTrader.Models;
using MachinaTrader.TradeManagers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;

namespace MachinaTrader
{
    public static class Globals
    {
        public static IApplicationBuilder GlobalApplicationBuilder;
        public static IServiceScope GlobalServiceScope { get; set; }
        public static IConfiguration GlobalConfiguration { get; set; }
        public static IDataStore GlobalDataStore { get; set; }
        public static IDataStoreBacktest GlobalDataStoreBacktest { get; set; }
        public static IExchangeApi GlobalExchangeApi { get; set; }
        public static IAppCache AppCache { get; set; }
        public static ILoggerFactory GlobalLoggerFactory { get; set; }
        public static CancellationToken GlobalTimerCancellationToken = new CancellationToken();
        public static IHubContext<HubMyntTraders> GlobalHubMyntTraders;
        public static IHubContext<HubMyntStatistics> GlobalHubMyntStatistics;
        public static IHubContext<HubMyntLogs> GlobalHubMyntLogs;
        public static IHubContext<HubMyntBacktest> GlobalHubMyntBacktest;
        public static RuntimeConfig RuntimeSettings = new RuntimeConfig();
        public static IScheduler QuartzTimer = new StdSchedulerFactory().GetScheduler().Result;
        public static TelegramNotificationOptions GlobalTelegramNotificationOptions { get; set; }
        public static ILogger TradeLogger;
        public static List<INotificationManager> NotificationManagers;
        public static OrderBehavior GlobalOrderBehavior;
        public static ConcurrentDictionary<string, Ticker> WebSocketTickers = new ConcurrentDictionary<string, Ticker>();
        public static MainConfig Configuration { get; set; }
        public static string ConfigFilePath = "MainConfig.json";

        public static List<string> GlobalCurrencys = new List<string>();
        public static List<string> ExchangeCurrencys = new List<string>();
    }

    /// <summary>
    /// Global Settings
    /// </summary>
    public class GlobalSettings
    {
        public async static void Init()
        {

            ILogger Log = Globals.GlobalLoggerFactory.CreateLogger<GlobalSettings>();

            Globals.TradeLogger = Globals.GlobalLoggerFactory.CreateLogger<TradeManager>();

            Globals.GlobalOrderBehavior = OrderBehavior.AlwaysFill;

            Globals.NotificationManagers = new List<INotificationManager>()
            {
                new SignalrNotificationManager(),
                new TelegramNotificationManager(Globals.GlobalTelegramNotificationOptions)
            };

            if (Globals.Configuration.SystemOptions.Database == "MongoDB")
            {
                Log.LogInformation("Database set to MongoDB");
                MongoDBOptions databaseOptions = new MongoDBOptions();
                Globals.GlobalDataStore = new MongoDBDataStore(databaseOptions);
                MongoDBOptions backtestDatabaseOptions = new MongoDBOptions();
                Globals.GlobalDataStoreBacktest = new MongoDBDataStoreBacktest(backtestDatabaseOptions);
            }
            else
            {
                Log.LogInformation("Database set to LiteDB");
                LiteDBOptions databaseOptions = new LiteDBOptions();
                Globals.GlobalDataStore = new LiteDBDataStore(databaseOptions);
                LiteDBOptions backtestDatabaseOptions = new LiteDBOptions();
                Globals.GlobalDataStoreBacktest = new LiteDBDataStoreBacktest(backtestDatabaseOptions);
            }

            // Global Hubs
            Globals.GlobalHubMyntTraders = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntTraders>>();
            Globals.GlobalHubMyntStatistics = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntStatistics>>();
            Globals.GlobalHubMyntLogs = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntLogs>>();
            Globals.GlobalHubMyntBacktest = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntBacktest>>();

            //Run Cron
            IScheduler scheduler = Globals.QuartzTimer;

            IJobDetail buyTimerJob = JobBuilder.Create<Timers.BuyTimer>()
                .WithIdentity("buyTimerJobTrigger", "buyTimerJob")
                .Build();

            ITrigger buyTimerJobTrigger = TriggerBuilder.Create()
                .WithIdentity("buyTimerJobTrigger", "buyTimerJob")
                .WithCronSchedule(Globals.Configuration.TradeOptions.BuyTimer)
                .UsingJobData("force", false)
                .Build();

            await scheduler.ScheduleJob(buyTimerJob, buyTimerJobTrigger);

            IJobDetail sellTimerJob = JobBuilder.Create<Timers.SellTimer>()
                .WithIdentity("sellTimerJobTrigger", "sellTimerJob")
                .Build();

            ITrigger sellTimerJobTrigger = TriggerBuilder.Create()
                .WithIdentity("sellTimerJobTrigger", "sellTimerJob")
                .WithCronSchedule(Globals.Configuration.TradeOptions.SellTimer)
                .UsingJobData("force", false)
                .Build();

            await scheduler.ScheduleJob(sellTimerJob, sellTimerJobTrigger);

            await scheduler.Start();
            Log.LogInformation($"Buy Cron will run at: {buyTimerJobTrigger.GetNextFireTimeUtc() ?? DateTime.MinValue:r}");
            Log.LogInformation($"Sell Cron will run at: {sellTimerJobTrigger.GetNextFireTimeUtc() ?? DateTime.MinValue:r}");
        }

        public static void LoadSettings()
        {
            // Check if Overrides exists
            var settingsStr = "appsettings.json";
            if (File.Exists("appsettings.overrides.json"))
                settingsStr = "appsettings.overrides.json";

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(settingsStr, optional: true);
            Globals.GlobalConfiguration = builder.Build();

            if (!File.Exists(Globals.ConfigFilePath))
            {
                //Init Global Config with default currency array
                Globals.Configuration = MergeObjects.MergeCsDictionaryAndSave(new MainConfig(), "MainConfig.json").ToObject<MainConfig>();
                Globals.Configuration.TradeOptions.MarketBlackList = new List<string> { };
                Globals.Configuration.TradeOptions.OnlyTradeList = new List<string> { "ETHBTC", "LTCBTC" };
                Globals.Configuration.TradeOptions.AlwaysTradeList = new List<string> { "ETHBTC", "LTCBTC" };
                var defaultExchangeOptions = new ExchangeOptions();
                defaultExchangeOptions.Exchange = Exchange.Binance;
                defaultExchangeOptions.ApiKey = "";
                defaultExchangeOptions.ApiSecret = "";
                Globals.Configuration.ExchangeOptions.Add(defaultExchangeOptions);
                Globals.Configuration = MergeObjects.MergeCsDictionaryAndSave(Globals.Configuration, "MainConfig.json", JObject.FromObject(Globals.Configuration)).ToObject<MainConfig>();

            }
            else
            {

                Globals.Configuration = MergeObjects.MergeCsDictionaryAndSave(new MainConfig(), "MainConfig.json").ToObject<MainConfig>();
            }

            var exchangeOption = Globals.Configuration.ExchangeOptions.FirstOrDefault();
            switch (exchangeOption.Exchange)
            {
                case Exchange.GdaxSimulation:
                    Globals.GlobalExchangeApi = new BaseExchange(exchangeOption, new SimulationExchanges.ExchangeGdaxSimulationApi());
                    exchangeOption.IsSimulation = true;
                    break;
                case Exchange.BinanceSimulation:
                    //Globals.GlobalExchangeApi = new BaseExchange(exchangeOption, new SimulationExchanges.ExchangeBinanceSimulationApi());
                    exchangeOption.IsSimulation = true;
                    break;
                default:
                    Globals.GlobalExchangeApi = new BaseExchange(exchangeOption);
                    exchangeOption.IsSimulation = false;
                    break;
            }

            //Websocket Test
            var fullApi = Globals.GlobalExchangeApi.GetFullApi().Result;

            //Create Exchange Currencies as List
            foreach (var currency in Globals.Configuration.TradeOptions.AlwaysTradeList)
            {
                Globals.GlobalCurrencys.Add(Globals.Configuration.TradeOptions.QuoteCurrency + "-" + currency);
            }

            foreach (var currency in Globals.GlobalCurrencys)
            {
                Globals.ExchangeCurrencys.Add(fullApi.GlobalSymbolToExchangeSymbol(currency));
            }

            if (!exchangeOption.IsSimulation)
                fullApi.GetTickersWebSocket(OnWebsocketTickersUpdated);

            // Telegram Notifications
            Globals.GlobalTelegramNotificationOptions = Globals.GlobalConfiguration.GetSection("Telegram").Get<TelegramNotificationOptions>();
        }



        public static void OnWebsocketTickersUpdated(IReadOnlyCollection<KeyValuePair<string, ExchangeSharp.ExchangeTicker>> updatedTickers)
        {
            foreach (var update in updatedTickers)
            {
                if (Globals.ExchangeCurrencys.Contains(update.Key))
                {
                    if (Globals.WebSocketTickers.TryGetValue(update.Key, out Ticker ticker))
                    {
                        ticker.Ask = update.Value.Ask;
                        ticker.Bid = update.Value.Bid;
                        ticker.Last = update.Value.Last;
                    }
                    else
                    {
                        Globals.WebSocketTickers.TryAdd(update.Key, new Ticker
                        {
                            Ask = update.Value.Ask,
                            Bid = update.Value.Bid,
                            Last = update.Value.Last
                        });
                    }
                }
            }
        }

        public sealed class QuartzScheduler
        {
            private static QuartzScheduler _instance;

            private static readonly object Padlock = new object();

            private readonly ISchedulerFactory _schedulerFactory;
            private readonly IScheduler _scheduler;

            QuartzScheduler()
            {
                _schedulerFactory = new StdSchedulerFactory();
                _scheduler = _schedulerFactory.GetScheduler().Result;
            }

            public static IScheduler Instance
            {
                get
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            _instance = new QuartzScheduler();
                        }
                        return _instance._scheduler;
                    }
                }
            }
        }

        /// <summary>
        /// Get System envirement information
        /// </summary>
        /// <returns></returns>
        public static string GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX";
            }

            return "Unknown";
        }

    }
}
