using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mynt.Core.Interfaces;
using Mynt.Core.Notifications;
using Mynt.Core.Strategies;
using Mynt.Core.TradeManagers;
using MachinaTrader.Helpers;
using MachinaTrader.TradeManagers;
using Quartz;

namespace MachinaTrader.Timers
{
    [DisallowConcurrentExecution]
    public class SellTimer : IJob
    {
        private static readonly ILogger Log = Runtime.GlobalLoggerFactory.CreateLogger<SellTimer>();

        /// <summary>
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual Task Execute(IJobExecutionContext context)
        {
            var tradeTradeManager = new TradeManager();

            Log.LogInformation("Mynt service is updating trades.");
            tradeTradeManager.UpdateExistingTrades();

            return Task.FromResult(true);
        }
    }
}
