using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mynt.Core.Models
{
    public class AccountBalance
    {
        private readonly string _currency;

        private readonly decimal _available;

        private readonly decimal _pending;

        public AccountBalance(string currency, decimal available, decimal pending)
        {
            this._currency = currency;
            this._available = available;
            this._pending = pending;
        }

        public string Currency => _currency;

        public decimal Balance => _available + _pending;

        public decimal Available => _available;

        public decimal Pending => _pending;
    }
}
