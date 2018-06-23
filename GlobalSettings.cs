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
        public static IHubContext<HubMyntStatistics> GlobalHubMyntStatistics;
        public static JObject RuntimeSettings = new JObject();

      // Creating TradeManager 
      Globals.GlobalExchangeApi = new BaseExchange(exchangeOptions);


      // Get TradeOptions
      var tradeOptions = Startup.Configuration.GetSection("TradeOptions").Get<TradeOptions>();
      //var tradeOptions = Globals.GlobalConfiguration.Get<TradeOptions>();

      // Get Strategy from appsettings.overrides.json
      var type = Type.GetType($"Mynt.Core.Strategies.{tradeOptions.DefaultStrategy}, Mynt.Core", true, true);
      var strategy = Activator.CreateInstance(type) as ITradingStrategy ?? new TheScalper();

      // Trading mode
        if (tradeOptions.PaperTrade)
        {
            // PaperTrader
            ILogger tradeLogger = Globals.GlobalLoggerFactory.CreateLogger<PaperTradeManager>();
            PaperTradeManager paperTradeManager = new PaperTradeManager(new BaseExchange(exchangeOptions), strategy, new SignalrNotificationManager(), tradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);
            var runTimer = new MyntHostedService(paperTradeManager, Globals.GlobalMyntHostedServiceOptions);

            // Start task
            await runTimer.StartAsync(Globals.GlobalTimerCancellationToken);
        }
        else
        {
            // LiveTrader
            ILogger tradeLogger = Globals.GlobalLoggerFactory.CreateLogger<LiveTradeManager>();
            LiveTradeManager liveTradeManager = new LiveTradeManager(new BaseExchange(exchangeOptions), strategy, new SignalrNotificationManager(), tradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);
            var runTimer = new MyntHostedService(liveTradeManager, Globals.GlobalMyntHostedServiceOptions);

            // Start task
            await runTimer.StartAsync(Globals.GlobalTimerCancellationToken);
        }
          
    }
=======
            // Creating TradeManager 
            Globals.GlobalExchangeApi = new BaseExchange(exchangeOptions);
            ILogger paperTradeLogger = Globals.GlobalLoggerFactory.CreateLogger<PaperTradeManager>();
            PaperTradeManager paperTradeManager = new PaperTradeManager(new BaseExchange(exchangeOptions), new FreqClassic(), new SignalrNotificationManager(), paperTradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);
            var runTimer = new MyntHostedService(paperTradeManager, Globals.GlobalMyntHostedServiceOptions);

            // Start task
            await runTimer.StartAsync(Globals.GlobalTimerCancellationToken);
        }
>>>>>>> 129322884c9bd3efa50c8b069b2e79bfd7e7f2d0

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
