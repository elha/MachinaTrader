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
using MyntUI.Helpers;
using MyntUI.Hosting;
using MyntUI.Hubs;
using Newtonsoft.Json.Linq;

namespace MyntUI
{
    public static class Globals
    {
        public static IApplicationBuilder GlobalApplicationBuilder;
        public static IServiceScope GlobalServiceScope { get; set; }
        public static IConfiguration GlobalConfiguration { get; set; }
        public static IDataStore GlobalDataStore { get; set; }
        public static TradeOptions GlobalTradeOptions { get; set; }
        public static MyntHostedServiceOptions GlobalMyntHostedServiceOptions { get; set; }
        public static ExchangeOptions GlobalExchangeOptions { get; set; }
        public static IExchangeApi GlobalExchangeApi { get; set; }
        public static ILoggerFactory GlobalLoggerFactory { get; set; }
        public static CancellationToken GlobalTimerCancellationToken = new CancellationToken();
        public static IHubContext<HubMyntTraders> GlobalHubMyntTraders;
        public static IHubContext<HubMyntTrades> GlobalHubMyntTrades;
        public static IHubContext<HubMyntStatistics> GlobalHubMyntStatistics;
        public static IHubContext<HubMyntLogs> GlobalHubMyntLogs;
        public static JObject RuntimeSettings = new JObject();
        public static TelegramNotificationOptions GlobalTelegramNotificationOptions { get; set; }

    }

    /// <summary>
    /// Global Settings
    /// </summary>
    public class GlobalSettings
    {
        public async static void Init()
        {
            // Runtime platform getter
            Globals.RuntimeSettings["platform"] = new JObject();
            Globals.RuntimeSettings["platform"]["os"] = GetOs();
            Globals.RuntimeSettings["platform"]["computerName"] = Environment.MachineName;
            Globals.RuntimeSettings["platform"]["userName"] = Environment.UserName;
            Globals.RuntimeSettings["platform"]["webInitialized"] = false;
            Globals.RuntimeSettings["platform"]["settingsInitialized"] = false;
            Globals.RuntimeSettings["signalrClients"] = new JObject();

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

            // Database options
            LiteDBOptions databaseOptions = new LiteDBOptions();
            Globals.GlobalDataStore = new LiteDBDataStore(databaseOptions);

            // Global Hubs
            Globals.GlobalHubMyntTraders = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntTraders>>();
            Globals.GlobalHubMyntTrades = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntTrades>>();
            Globals.GlobalHubMyntStatistics = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntStatistics>>();
            Globals.GlobalHubMyntLogs = Globals.GlobalServiceScope.ServiceProvider.GetService<IHubContext<HubMyntLogs>>();

            // Get Strategy from appsettings.overrides.json
            var type = Type.GetType($"Mynt.Core.Strategies.{Globals.GlobalTradeOptions.DefaultStrategy}, Mynt.Core", true, true);
            var strategy = Activator.CreateInstance(type) as ITradingStrategy ?? new FreqTrade();

            // Trading mode  Configuration.GetSection("Telegram").Get<TelegramNotificationOptions>()) 
            var notificationManagers = new List<INotificationManager>()
            {
                new SignalrNotificationManager(),
                new TelegramNotificationManager(Globals.GlobalTelegramNotificationOptions)
            };

            if (Globals.GlobalTradeOptions.PaperTrade)
            {
                // PaperTrader
                ILogger tradeLogger = Globals.GlobalLoggerFactory.CreateLogger<PaperTradeManager>();
                var paperTradeManager = new PaperTradeManager(Globals.GlobalExchangeApi, strategy, notificationManagers[0], tradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);
                var runTimer = new MyntHostedService(paperTradeManager, Globals.GlobalMyntHostedServiceOptions);

                // Start task
                await runTimer.StartAsync(Globals.GlobalTimerCancellationToken);
            }
            else
            {
                // LiveTrader
                ILogger tradeLogger = Globals.GlobalLoggerFactory.CreateLogger<LiveTradeManager>();
                var liveTradeManager = new LiveTradeManager(Globals.GlobalExchangeApi, strategy, notificationManagers[0], tradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);
                var runTimer = new MyntHostedService(liveTradeManager, Globals.GlobalMyntHostedServiceOptions);

                // Start task
                await runTimer.StartAsync(Globals.GlobalTimerCancellationToken);
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
