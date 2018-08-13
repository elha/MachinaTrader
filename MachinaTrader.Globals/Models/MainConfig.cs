using System.Collections.Generic;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Globals.Models
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
        public string Database { get; set; } = "MongoDB";
        public string DefaultUserName { get; set; } = "admin";
        public string DefaultUserEmail { get; set; } = "admin@localhost";
        public string DefaultUserPassword { get; set; } = "admin";
        public string Theme { get; set; } = "dark";
    }

}
