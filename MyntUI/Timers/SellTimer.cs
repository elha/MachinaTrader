using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mynt.Core.Interfaces;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using Mynt.Core.TradeManagers;
using MyntUI.Helpers;
using Quartz;

namespace MyntUI.Timers
{
    [DisallowConcurrentExecution]
    public class SellTimer : IJob
    {
        private static readonly ILogger Log = Globals.GlobalLoggerFactory.CreateLogger<SellTimer>();

        /// <summary>
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual Task Execute(IJobExecutionContext context)
        {
            var type = Type.GetType($"Mynt.Core.Strategies.{Globals.GlobalTradeOptions.DefaultStrategy}, Mynt.Core", true, true);
            var strategy = Activator.CreateInstance(type) as ITradingStrategy ?? new TheScalper();

            var notificationManagers = new List<INotificationManager>()
            {
                new SignalrNotificationManager(),
                new TelegramNotificationManager(Globals.GlobalTelegramNotificationOptions)
            };

            ILogger tradeLogger = Globals.GlobalLoggerFactory.CreateLogger<HybridTradeManager>();
            var hybridTradeManager = new HybridTradeManager(Globals.GlobalExchangeApi, strategy, notificationManagers, tradeLogger, Globals.GlobalTradeOptions, Globals.GlobalDataStore);

            Log.LogInformation("Mynt service is updating trades.");
            hybridTradeManager.UpdateExistingTrades();

            return Task.FromResult(true);
        }
    }
}
