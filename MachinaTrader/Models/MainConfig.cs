using Mynt.Core.Exchanges;
using System.Collections.Generic;
using TradeOptions = MachinaTrader.TradeManagers.TradeOptions;
using Mynt.Core.Notifications;

namespace MachinaTrader.Models
{
    public class MainConfig
    {
        public SystemOptions SystemOptions = new SystemOptions();
        public TradeOptions TradeOptions = new TradeOptions();
        public TelegramNotificationOptions TelegramOptions = new TelegramNotificationOptions();
        public List<ExchangeOptions> ExchangeOptions = new List<ExchangeOptions> { };
    }

    public class SystemOptions
    {
        public int WebPort { get; set; } = 5000;
        public string Database { get; set; } = "LiteDB";
        public string DefaultUserName { get; set; } = "admin";
        public string DefaultUserEmail { get; set; } = "admin@localhost";
        public string DefaultUserPassword { get; set; } = "admin";
    }

}
