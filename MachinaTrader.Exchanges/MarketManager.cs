using System;
using System.Collections.Concurrent;
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
        public class SignalStat
        {
            public DateTime TimeStamp = DateTime.UtcNow;
            public int Buys = 0;
            public int Sells = 0;
        }

        private static string[] mAllowedQuote = new string[] { "BTC", "USD", "USDT" };
        public static  ConcurrentDictionary<string, TradeMarket> Markets = new ConcurrentDictionary<string, TradeMarket>();
        public static List<SignalStat> SignalStats = new List<SignalStat>();

        static DateTime nLastStatus = DateTime.MinValue;

        public static ITradingStrategy StrategyUp;

        public static ITradingStrategy StrategySide;

        public static bool NeedsUpdate()
        {
            return Markets.Count == 0;
        }

        public static void Update()
        {
            var arrMarkets = Global.ExchangeApi.GetMarketSummaries(null).Result.Where(m => mAllowedQuote.Any(c => c == m.CurrencyPair.QuoteCurrency));

            var Symbols = Global.Configuration.TradeOptions.TradeAssetsList().ToList();

            Parallel.ForEach(arrMarkets, market =>
            {
                if (!Markets.ContainsKey(market.GlobalMarketName))
                    Markets[market.GlobalMarketName] = new TradeMarket()
                    {
                        Exchange = Global.ExchangeApi.GetFullApi().Name,
                        GlobalMarketName = market.GlobalMarketName,
                        CurrencyPair = market.CurrencyPair,
                        SettleCurrency = market.SettleCurrency,
                        LotSize = market.LotSize,
                        Fee = market.Fee
                    };

                var m = Markets[market.GlobalMarketName];
                m.Update(market, Symbols.Count == 0 || Symbols.Any(c => c == m.GlobalMarketName));
            });

            SignalStats.Add(new SignalStat()
            {
                Buys = Markets.Values.Count(m => m.Active && m.LastStrategyAdvice?.Advice == Globals.Structure.Enums.TradeAdviceEnum.Buy),
                Sells = Markets.Values.Count(m => m.Active && m.LastStrategyAdvice?.Advice == Globals.Structure.Enums.TradeAdviceEnum.Sell)
            });

            CalcTrend();

            var bgdt = GlobalTrend;
            if (bgdt != gbgdt)
            {
                Global.Logger.Information($"Trendinfo Change {gbgdt}>{bgdt}: {MarketManager.TrendMessage()}");
                gbgdt = bgdt;
                LastTrendChange = DateTime.UtcNow;
            }

            if ((DateTime.UtcNow - nLastStatus).TotalMinutes > 3)
            {
                nLastStatus = DateTime.UtcNow;
                Global.Logger.Information($"Riskinfo Current {DepotManager.RiskValue:N2}, Max {DepotManager.MaxRiskValue:N2}");
                Global.Logger.Information($"Trendinfo Current {MarketManager.TrendMessage()}");
                Global.Logger.Information($"Marketinfo #{Markets.Values.Where(m => m.Active).Count()} {String.Join(",", Markets.Where(m => m.Value.CurrencyPair.QuoteCurrency == "USD").Select(m => m.Value.GlobalMarketName + "(" + m.Value.GetTrendInfo() + ")").ToArray())}");
                SignalStats = SignalStats.Where(s => (DateTime.UtcNow - s.TimeStamp).TotalMinutes < 30).ToList();
            }
        }

        static Trend gbgdt = Trend.side;
        static DateTime LastTrendChange = DateTime.UtcNow;

        private static object TrendMessage()
        {
            var stats = SignalStats.Where(s => (DateTime.UtcNow - s.TimeStamp).TotalMinutes < 3);
            return $"{gbgdt} ({(DateTime.UtcNow - LastTrendChange).TotalMinutes :N0} min)  {Markets.Count(m => m.Value.MarketTrend == Trend.up)}/{Markets.Count(m => m.Value.MarketTrend == Trend.side)}/{Markets.Count(m => m.Value.MarketTrend == Trend.down)} USDtrends / {stats.Sum(s => s.Buys)} Buys / {stats.Sum(s => s.Sells)} Sells";
        }

        public enum Trend
        {
            up,
            side,
            caution,
            down
        }

        public static Trend GlobalTrend;
        public static int DownCount;
        public static DateTime CautionUntil;
        public static decimal GlobalTrendScore;

        public static void CalcTrend()
        {
            var iDownCount = Markets.Count(m => m.Value.MarketTrend == MarketManager.Trend.down);
            if (GlobalTrend == Trend.up && DownCount == 0 && iDownCount > 0)
                CautionUntil = DateTime.UtcNow.AddMinutes(60);

            if (iDownCount >= (int)(Markets.Count() * 0.12m))
                GlobalTrend = MarketManager.Trend.down;
            //else if (CautionUntil > DateTime.UtcNow)
            //    GlobalTrend = MarketManager.Trend.caution;
            else if (Markets.Count(m => m.Value.MarketTrend == Trend.up) >= (int)(Markets.Count() * 0.25m))
                GlobalTrend = MarketManager.Trend.up;
            else
                GlobalTrend = MarketManager.Trend.side;

            DownCount = iDownCount;

            GlobalTrendScore = Markets.Count(m => m.Value.MarketTrend == Trend.up) - Markets.Count(m => m.Value.MarketTrend == Trend.down);
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
                if (Currency != "USD" && Currency != "USDT" && !Markets.Any(m => m.Value.CurrencyPair.BaseCurrency == Currency && m.Value.CurrencyPair.QuoteCurrency == "BTC"))
                   return 0m;

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
