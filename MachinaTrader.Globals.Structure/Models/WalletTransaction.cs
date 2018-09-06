using System;

namespace MachinaTrader.Globals.Structure.Models
{
    public class WalletTransaction
    {
        public WalletTransaction()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }          
    }
}
