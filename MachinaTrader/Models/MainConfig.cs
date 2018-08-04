using Mynt.Core.Enums;
using Mynt.Core.Exchanges;
using Mynt.Core.TradeManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachinaTrader.Models
{
    public class MainConfig
    {
        public SystemOptions SystemOptions = new SystemOptions();
        public MachinaTrader.TradeManagers.TradeOptions TradeOptions = new MachinaTrader.TradeManagers.TradeOptions();
        public List<ExchangeOptions> ExchangeOptions = new List<ExchangeOptions> { };
    }

    public class SystemOptions
    {
        public int WebPort { get; set; } = 5000;
        public string Database { get; set; } = "LiteDB";
    }
}
