using ExchangeSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mynt.Core.Exchanges
{
    public class ExchangeBacktestAPI : ExchangeAPI
    {
        public override string BaseUrl { get => "local"; set => throw new NotImplementedException(); }

        public override string Name => "ExchangeBacktestAPI";
    }
}
