using System.Threading.Tasks;
using MachinaTrader.TradeManagers;
using Quartz;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.SignalR;
using System;

namespace MachinaTrader.Timers
{
    [DisallowConcurrentExecution]
    public class TradeTimer : IJob
    {
        /// <summary>
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            var tradeManager = new TradeManagerBasket();

            await tradeManager.Run();

            await Runtime.GlobalHubTraders.Clients.All.SendAsync("Send", $"TradeTimer {DateTime.UtcNow.ToString("HH:mm:ss")}");
            await Task.FromResult(true);
        }
    }
}
