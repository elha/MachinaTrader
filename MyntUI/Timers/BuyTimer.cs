using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mynt.Core.Interfaces;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using MyntUI.Helpers;
using MyntUI.TradeManagers;
using Quartz;

namespace MyntUI.Timers
{
    [DisallowConcurrentExecution]
    public class BuyTimer : IJob
    {
        private static readonly ILogger Log = Globals.GlobalLoggerFactory.CreateLogger<BuyTimer>();

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
