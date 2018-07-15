using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mynt.Core.Enums;
using Mynt.Core.Exchanges;
using Mynt.Core.Interfaces;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using Mynt.Core.TradeManagers;
using Mynt.Data.LiteDB;
using Mynt.Data.MongoDB;
using MyntUI.Helpers;
using MyntUI.Hosting;
using MyntUI.Hubs;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;

namespace MyntUI
{
    public static class Globals
    {
        public static IApplicationBuilder GlobalApplicationBuilder;
        public static IServiceScope GlobalServiceScope { get; set; }
        public static IConfiguration GlobalConfiguration { get; set; }
        public static IDataStore GlobalDataStore { get; set; }
        public static IDataStoreBacktest GlobalDataStoreBacktest { get; set; }
        public static TradeOptions GlobalTradeOptions { get; set; }
        public static MyntHostedServiceOptions GlobalMyntHostedServiceOptions { get; set; }
        public static ExchangeOptions GlobalExchangeOptions { get; set; }
        public static IExchangeApi GlobalExchangeApi { get; set; }
        public static ILoggerFactory GlobalLoggerFactory { get; set; }
        public static CancellationToken GlobalTimerCancellationToken = new CancellationToken();
        public static IHubContext<HubMyntTraders> GlobalHubMyntTraders;
        public static IHubContext<HubMyntStatistics> GlobalHubMyntStatistics;
        public static IHubContext<HubMyntLogs> GlobalHubMyntLogs;
        public static IHubContext<HubMyntBacktest> GlobalHubMyntBacktest;
        public static JObject RuntimeSettings = new JObject();
        public static IScheduler QuartzTimer = new StdSchedulerFactory().GetScheduler().Result;
        public static TelegramNotificationOptions GlobalTelegramNotificationOptions { get; set; }

    }

    /// <summary>
    /// Global Settings
    /// </summary>
    public class GlobalSettings
    {
        public async static void Init()
        {

            ILogger Log = Globals.GlobalLoggerFactory.CreateLogger<GlobalSettings>();

            // Runtime platform getter
            Globals.RuntimeSettings["platform"] = new JObject();
            Globals.RuntimeSettings["platform"]["os"] = GetOs();
            Globals.RuntimeSettings["platform"]["computerName"] = Environment.MachineName;
            Globals.RuntimeSettings["platform"]["userName"] = Environment.UserName;
            Globals.RuntimeSettings["platform"]["webInitialized"] = false;
            Globals.RuntimeSettings["platform"]["settingsInitialized"] = false;
            Globals.RuntimeSettings["signalrClients"] = new JObject();

            // Database options
            LiteDBOptions databaseOptions = new LiteDBOptions();
            Globals.GlobalDataStore = new LiteDBDataStore(databaseOptions);

            // Database Backtester
            LiteDBOptions backtestDatabaseOptions = new LiteDBOptions();
            Globals.GlobalDataStoreBacktest = new LiteDBDataStoreBacktest(backtestDatabaseOptions);
            
            /*
            // Database options
            MongoDBOptions databaseOptions = new MongoDBOptions();
            Globals.GlobalDataStore = new MongoDBDataStore(databaseOptions);

            // Database Backtester
            MongoDBOptions backtestDatabaseOptions = new MongoDBOptions();
            Globals.GlobalDataStoreBacktest = new MongoDBDataStoreBacktest(backtestDatabaseOptions);
            */

            LoadSettings();


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
                .WithCronSchedule(Globals.GlobalMyntHostedServiceOptions.BuyTimer)
                .UsingJobData("force", false)
                .Build();

            await scheduler.ScheduleJob(buyTimerJob, buyTimerJobTrigger);

            IJobDetail sellTimerJob = JobBuilder.Create<Timers.SellTimer>()
                .WithIdentity("sellTimerJobTrigger", "sellTimerJob")
                .Build();

            ITrigger sellTimerJobTrigger = TriggerBuilder.Create()
                .WithIdentity("sellTimerJobTrigger", "sellTimerJob")
                .WithCronSchedule(Globals.GlobalMyntHostedServiceOptions.SellTimer)
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
            Globals.GlobalTradeOptions = Globals.GlobalConfiguration.GetSection("TradeOptions").Get<TradeOptions>();
            Globals.GlobalExchangeOptions = Globals.GlobalConfiguration.Get<ExchangeOptions>();
            Globals.GlobalExchangeApi = new BaseExchange(Globals.GlobalExchangeOptions);
            Globals.GlobalMyntHostedServiceOptions = Globals.GlobalConfiguration.GetSection("Hosting").Get<MyntHostedServiceOptions>();

            // Telegram Notifications
            Globals.GlobalTelegramNotificationOptions = Globals.GlobalConfiguration.GetSection("Telegram").Get<TelegramNotificationOptions>();

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
