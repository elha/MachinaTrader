using System;
using System.Collections.Generic;
using System.Text;
using MachinaTrader.Globals;

namespace MachinaTrader.Exchanges
{
    public static class DepotManager
    {
        public static decimal PositionSize { get
            {
                return Global.Configuration.TradeOptions.AmountToInvestPerTrade;
            }
        }

        public static decimal MaxRiskValue { get
            {
                return mESTOPIfLossOverPosPercent * PositionSize / 100m;
            }
        }

        public static decimal MaxRiskPercentage {  get
            {
                return mESTOPIfLossOverPosPercent;
            }
        }

        public static Dictionary<string, decimal> Balances = new Dictionary<string, decimal>();
        public static decimal mDCAPosPercent = 0.2m;
        public static decimal mESTOPIfLossOverPosPercent = 0.15m;
        public static decimal RiskValue = 0m;

        public static void Update()
        {
            var api = Global.ExchangeApi.GetFullApi();
            Balances = api.GetAmountsAsync().Result;
        }

        public static bool HasPosition(string currency)
        {
            // if min 20% position exists
            var basebalance = 0m;
            Balances.TryGetValue(currency, out basebalance);
            var baseusd = MarketManager.GetUSD(currency, basebalance);
            return (baseusd > PositionSize * 0.2m);
        }

        public static decimal GetPositionSize(string currency, decimal fee)
        {
            var balance = 0m;
            Balances.TryGetValue(currency, out balance);
            var usd = MarketManager.GetUSD(currency, balance);

            var nPercent = 0m;
            if (usd < 50m)
                nPercent = 0m;
            else if (usd < PositionSize * 1.5m)
                nPercent = 1m - fee;  // no fractions below 50% Position
            else
                nPercent = PositionSize / usd;  // one position

            return nPercent * balance;
        }
    }
}
