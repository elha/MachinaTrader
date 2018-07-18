using Mynt.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mynt.Core.TradeManagers
{
    public class BacktestTradeManager : ITradeManager
    {
        public Task LookForNewTrades()
        {
            throw new NotImplementedException();
        }

        public Task UpdateExistingTrades()
        {
            throw new NotImplementedException();
        }
    }
}
