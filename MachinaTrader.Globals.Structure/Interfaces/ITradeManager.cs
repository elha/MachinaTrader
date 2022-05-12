using System.Threading.Tasks;

namespace MachinaTrader.Globals.Structure.Interfaces
{
    public interface ITradeManager
    {
        /// <summary>
        /// Checks if new trades can be started.
        /// </summary>
        /// <returns></returns>
        Task Run(string strategyString = null);

    }
}
