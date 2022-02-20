using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;

namespace MachinaTrader.Exchanges
{

    public static class MarketManager
    {

        private static string[] mAllowedQuote = new string[] { "BTC", "USD", "USDT" };
        public static  Dictionary<string, TradeMarket> Markets = new Dictionary<string, TradeMarket>();


        public static ITradingStrategy Strategy;

        public static bool NeedsUpdate()
        {
            return Markets.Count == 0;
        }

        public static void Update()
        {
            var arrMarkets = Global.ExchangeApi.GetMarketSummaries(null).Result.Where(m => mAllowedQuote.Any(c => c == m.CurrencyPair.QuoteCurrency));

            var Symbols = Global.Configuration.TradeOptions.TradeAssetsList().ToList();

            foreach (var market in arrMarkets)
            {
                if (!Markets.ContainsKey(market.GlobalMarketName)) Markets.Add(market.GlobalMarketName, new TradeMarket()
                {
                    GlobalMarketName = market.GlobalMarketName,
                    CurrencyPair = market.CurrencyPair,
                    SettleCurrency = market.SettleCurrency,
                    LotSize = market.LotSize,
                    Fee = market.Fee
                }) ;

                var m = Markets[market.GlobalMarketName];
                m.Update(market, Symbols.Count == 0 || Symbols.Any(c => c == m.GlobalMarketName));
            }

            if (DateTime.Now.Minute == 0)
            {
                Global.Logger.Information($"Marketinfo #{Markets.Values.Where(m => m.Active).Count()} {String.Join(",", Markets.Select(m=>m.Value.GlobalMarketName).ToArray())}");               
            }
        }

        public static void SaveToDB()
        {
            Parallel.ForEach(Markets, async (m) =>
            {
                try
                {
                    await m.Value.SaveToDB();
                }
                catch (Exception ex) { }
            });
        }

        public static decimal GetUSD(string Currency, decimal value)
        {
            if (Currency == "USD" || Currency == "USDT") return value;
            if (NeedsUpdate()) Update();
            var market = Markets.FirstOrDefault(m => m.Value.CurrencyPair.BaseCurrency == Currency &&
            (m.Value.CurrencyPair.QuoteCurrency == "USD" || m.Value.CurrencyPair.QuoteCurrency == "USDT")
            );
            if(market.Value == null)
            {
                var valbtc = GetBTC(Currency, value);
                return GetUSD("BTC", valbtc);
            }
            return market.Value.Last.Close * value;
        }

        public static decimal GetUSD(AccountBalance balance)
        {
            return GetUSD(balance.Currency, balance.Available);
        }

        public static decimal GetBTC(string Currency, decimal value)
        {
            if (Currency == "BTC" || Currency == "XBT") return value;
            if (NeedsUpdate()) Update();
            var market = Markets.FirstOrDefault(m => m.Value.CurrencyPair.BaseCurrency == Currency && m.Value.CurrencyPair.QuoteCurrency == "BTC");
            if (market.Value == null)
            {
                var valusd = GetUSD(Currency, value);
                return valusd / GetUSD("BTC", 1);
            }
            return market.Value.Last.Close * value;
        }

        public static decimal GetBTC(AccountBalance balance)
        {
            return GetBTC(balance.Currency, balance.Available);
        }

        public static decimal GetConversion(string Currency, decimal value, string target)
        {
            if (Currency == target) return value;
            if (NeedsUpdate()) Update();

            var valusd = GetUSD(Currency, value);
            var conversion = GetUSD(target, 1 );
            if (conversion == 0m) return 0m;
            return valusd / conversion;
        }
    }
}
