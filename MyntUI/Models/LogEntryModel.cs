using System;
using System.Collections.Generic;
using System.Linq;
using Mynt.Core.Models;
using Mynt.Core.TradeManagers;

namespace MyntUI.Models
{
    public class LogEntry
    {
        // Profit - loss
        public DateTime Date { get; set; }
        public string LogState { get; set; }
        public string Msg { get; set; }
    }
}
