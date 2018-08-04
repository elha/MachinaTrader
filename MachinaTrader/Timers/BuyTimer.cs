using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mynt.Core.Interfaces;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using MachinaTrader.Helpers;
using MachinaTrader.TradeManagers;
using Quartz;

namespace MachinaTrader.Timers
{
    [DisallowConcurrentExecution]
    public class BuyTimer : IJob
    {
        private static readonly ILogger Log = Runtime.GlobalLoggerFactory.CreateLogger<BuyTimer>();

        /// <summary>
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual Task Execute(IJobExecutionContext context)
        {
            var tradeManager = new TradeManager();

            Log.LogInformation("Mynt service is looking for new trades.");
            tradeManager.LookForNewTrades();

            return Task.FromResult(true);
        }
    }
}
