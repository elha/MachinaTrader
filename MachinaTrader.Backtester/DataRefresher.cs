using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Force.DeepCloner;
using MachinaTrader.Exchanges;
using MachinaTrader.Globals.Helpers;
using MachinaTrader.Globals.Structure.Extensions;
using MachinaTrader.Globals.Structure.Interfaces;
using MachinaTrader.Globals.Structure.Models;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Backtester
{
    public class DataRefresher
    {
        public static Dictionary<string, BacktestOptions> CurrentlyRunningUpdates = new Dictionary<string, BacktestOptions>();

        public static async Task<bool> CheckForCandleData(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            List<string> allDatabases = await dataStore.GetBacktestAllDatabases(backtestOptions);
            if (allDatabases.Count == 0)
            {
                return false;
            }
            return true;
        }

        public static async Task RefreshCandleData(Action<string> callback, BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            BaseExchange baseExchangeApi = new BaseExchangeInstance().BaseExchange(backtestOptions.Exchange.ToString());

            //var cts = new CancellationTokenSource();
            //var parallelOptions = new ParallelOptions();
            //parallelOptions.CancellationToken = cts.Token;
            //parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
            //Parallel.ForEach(backtestOptions.Coins, parallelOptions, async globalSymbol =>
            //{
            foreach (string globalSymbol in backtestOptions.Coins)
            {
                string exchangeSymbol = await baseExchangeApi.GlobalSymbolToExchangeSymbol(globalSymbol);
                backtestOptions.Coin = globalSymbol;
                string currentlyRunningString = backtestOptions.Exchange + "_" + globalSymbol + "_" + backtestOptions.CandlePeriod;
                lock (CurrentlyRunningUpdates)
                {
                    if (CurrentlyRunningUpdates.ContainsKey(currentlyRunningString))
                    {
                        callback($"\tUpdate still in process:  {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} from {backtestOptions.StartDate} UTC to {DateTime.UtcNow.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                        return;
                    }
                    CurrentlyRunningUpdates[currentlyRunningString] = backtestOptions;
                }

                DateTime startDate = Convert.ToDateTime(backtestOptions.StartDate).ToUniversalTime();
                DateTime endDate = DateTime.UtcNow;
                bool databaseExists = true;

                // Delete an existing file if this is no update
                if (!backtestOptions.UpdateCandles)
                {
                    dataStore.DeleteBacktestDatabase(backtestOptions).RunSynchronously();
                    callback($"\tRecreate database: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                }
                else
                {
                    //candleCollection.EnsureIndex("Timestamp");
                    Candle databaseLastCandle = await dataStore.GetBacktestLastCandle(backtestOptions);
                    if (databaseLastCandle != null)
                    {
                        startDate = databaseLastCandle.Timestamp.ToUniversalTime();
                        callback($"\tUpdate database: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                    }
                    else
                    {
                        callback($"\tCreate database: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                        databaseExists = false;
                    }
                }

                if (startDate == endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod)))
                {
                    callback($"\tAlready up to date: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                    lock (CurrentlyRunningUpdates)
                    {
                        CurrentlyRunningUpdates.Remove(currentlyRunningString);
                    }
                    return;
                }

                // Get these in batches of 500 because they're limited in the API.
                while (startDate < endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod)))
                {
                    try
                    {
                        //List<Candle> candles = await baseExchangeApi.GetChunkTickerHistory(exchangeSymbol, backtestOptions.CandlePeriod.FromMinutesEquivalent(), startDate, endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod)));
                        List<Candle> candles = await baseExchangeApi.GetTickerHistory(exchangeSymbol,
                                                                                      backtestOptions.CandlePeriod.FromMinutesEquivalent(),
                                                                                      startDate,
                                                                                      endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod)));

                        if (candles.Count == 0 || candles.Last().Timestamp.ToUniversalTime() == startDate)
                        {
                            callback($"\tNo update: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                            break;
                        }
                        startDate = candles.Last().Timestamp.ToUniversalTime();

                        if (!databaseExists)
                        {
                            await dataStore.SaveBacktestCandlesBulk(candles, backtestOptions);
                            databaseExists = true;
                        }
                        else
                        {
                            await dataStore.SaveBacktestCandlesBulkCheckExisting(candles, backtestOptions);
                        }

                        callback($"\tUpdated: {backtestOptions.Exchange.ToString()} with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} {startDate.ToUniversalTime()} to {endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                    }
                    catch (Exception e)
                    {
                        callback($"\tError while updating: {backtestOptions.Exchange.ToString()} {globalSymbol}: {e.Message}");
                        break;
                    }
                }
                lock (CurrentlyRunningUpdates)
                {
                    CurrentlyRunningUpdates.Remove(currentlyRunningString);
                }
            }
        }

        public static async Task FillCandlesGaps(Action<string> callback, BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            BaseExchange baseExchangeApi = new BaseExchangeInstance().BaseExchange(backtestOptions.Exchange.ToString());

            foreach (string globalSymbol in backtestOptions.Coins)
            {
                string exchangeSymbol = await baseExchangeApi.GlobalSymbolToExchangeSymbol(globalSymbol);
                backtestOptions.Coin = globalSymbol;

                string currentlyRunningString = backtestOptions.Exchange + "_" + globalSymbol + "_" + backtestOptions.CandlePeriod;
                lock (CurrentlyRunningUpdates)
                {
                    if (CurrentlyRunningUpdates.ContainsKey(currentlyRunningString))
                    {
                        callback($"\tUpdate still in process:  {backtestOptions.Exchange.ToString()} " +
                                 $"with Period {backtestOptions.CandlePeriod.ToString()}min for {globalSymbol} " +
                                 $"from {backtestOptions.StartDate} UTC to {DateTime.UtcNow.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod))} UTC");
                        return;
                    }
                    CurrentlyRunningUpdates[currentlyRunningString] = backtestOptions;
                }

                //current db candles
                var data = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);
                Candle currentHistoricalDataFirst = data.FirstOrDefault();
                Candle currentHistoricalDataLast = data.LastOrDefault();

                //there are data
                if (currentHistoricalDataFirst != null && currentHistoricalDataLast != null)
                {
                    var dataFirst = currentHistoricalDataFirst.Timestamp.ToUniversalTime();
                    var dataLast = currentHistoricalDataLast.Timestamp.ToUniversalTime();
                    var minutes = dataLast.Subtract(dataFirst).TotalMinutes;
                    var numOfExpectedCandles = minutes / backtestOptions.CandlePeriod + 1;
                    var numOfCandles = data.Count();

                    callback($"\t- Check database\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                               $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                               $"\tfrom {dataFirst} \tto {dataLast} UTC " +
                               $"expected:{numOfExpectedCandles}, have:{numOfCandles}");

                    if ((numOfExpectedCandles / numOfCandles) != 1) //found holes/multiple overall
                    {
                        callback($"\tFound overall holes\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                 $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                 $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC " +
                                 $"expected:{numOfExpectedCandles}, have:{numOfCandles}");

                        DateTime startDate = dataFirst;
                        DateTime endDate = dataLast;

                        //date range inspector
                        while (startDate < endDate.RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod)))
                        {
                            backtestOptions.StartDate = startDate;
                            backtestOptions.EndDate = backtestOptions.StartDate.AddHours(1).RoundDown(TimeSpan.FromMinutes(backtestOptions.CandlePeriod));

                            //candles of sub-period
                            var data2 = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);

                            var minutes2 = backtestOptions.EndDate.Subtract(backtestOptions.StartDate).TotalMinutes;
                            var numOfExpectedCandles2 = (minutes2 / backtestOptions.CandlePeriod) + 1;
                            var numOfCandles2 = data2.Count();

                            callback($"\tCheck period\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                     $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                     $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC " +
                                     $"expected:{numOfExpectedCandles2}, have:{numOfCandles2} ");

                            //normalize dates

                            var expectedCandlesList = new List<DateTime>();
                            for (int i = 0; i < numOfExpectedCandles2; i++)
                            {
                                var t = backtestOptions.StartDate.AddMinutes(i * (int)backtestOptions.CandlePeriod.FromMinutesEquivalent());
                                expectedCandlesList.Add(t);
                            }

                            var newCandlesToAdd = new List<Candle>();
                            foreach (var expectedItem in expectedCandlesList)
                            {
                                Candle newAdd = null;

                                long min = long.MaxValue;
                                foreach (var dbItem in data2)
                                {
                                    var d = Math.Abs(expectedItem.Subtract(dbItem.Timestamp).Ticks);

                                    if (d == 0) //exact date
                                        continue;

                                    if (d < min)
                                    {
                                        min = d;

                                        var n = dbItem.DeepClone();
                                        n.Timestamp = expectedItem;
                                        newAdd = n;
                                    }
                                }

                                //if distance is less then period (e.g. 15min) 
                                if (newAdd != null && TimeSpan.FromTicks(min).TotalMinutes <= (int)backtestOptions.CandlePeriod.FromMinutesEquivalent() / 2)
                                    newCandlesToAdd.Add(newAdd);
                            }

                            if (newCandlesToAdd.Any())
                            {
                                foreach (var dbItem in data2)
                                {
                                    callback($"\t b dbItem: {dbItem.Timestamp} {dbItem.Close}");
                                }

                                await dataStore.DeleteBacktestCandles(backtestOptions);
                                await dataStore.SaveBacktestCandlesBulkCheckExisting(newCandlesToAdd, backtestOptions);
                                data2 = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);

                                foreach (var dbItem in data2)
                                {
                                    callback($"\t a dbItem: {dbItem.Timestamp} {dbItem.Close}");
                                }
                            }

                            if (numOfCandles2 == 0 || (numOfExpectedCandles2 / numOfCandles2) != 1) //found holes/multiple in period or no candles
                            {
                                if (numOfCandles2 < numOfExpectedCandles2) //holes or nothing
                                {
                                    callback($"\tFound holes\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                                $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                                $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC " +
                                                $"expected:{numOfExpectedCandles2}, have:{numOfCandles2} ");
                                    try
                                    {
                                        List<Candle> candlesToAdd = await baseExchangeApi.GetTickerHistory(exchangeSymbol,
                                                                                                           backtestOptions.CandlePeriod.FromMinutesEquivalent(),
                                                                                                           backtestOptions.StartDate,
                                                                                                           backtestOptions.EndDate);

                                        //start tmp log
                                        foreach (var dbItem in data2)
                                        {
                                            callback($"\t dbItem: {dbItem.Timestamp} {dbItem.Close}");
                                        }

                                        for (int i = 0; i < numOfExpectedCandles2; i++)
                                        {
                                            var t = backtestOptions.StartDate.AddMinutes(i * (int)backtestOptions.CandlePeriod.FromMinutesEquivalent());
                                            callback($"\t expenctedItem: {t} ");
                                        }

                                        foreach (var excItem in candlesToAdd)
                                        {
                                            callback($"\t excItem: {excItem.Timestamp} {excItem.Close}");
                                        }
                                        //end tmp log

                                        if (candlesToAdd?.Count < numOfExpectedCandles2)//got less candle that required
                                        {
                                            callback($"\tNot enought data from Exchange\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                                      $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                                      $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC " +
                                                      $"expected:{numOfExpectedCandles2}, have:{numOfCandles2} ");

                                            Candle lastPeriodCandle = null;

                                            //exchange doesn't give data for period
                                            if (candlesToAdd?.Count == 0)
                                            {
                                                if (data2.Count == 0) //no data on db
                                                {
                                                    //precedent period
                                                    var backtestOptions2 = new BacktestOptions
                                                    {
                                                        DataFolder = backtestOptions.DataFolder,
                                                        Exchange = backtestOptions.Exchange,
                                                        Coin = backtestOptions.Coin,
                                                        CandlePeriod = backtestOptions.CandlePeriod,
                                                        EndDate = backtestOptions.StartDate
                                                    };

                                                    //last candle of precedent period
                                                    var c = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions2);

                                                    lastPeriodCandle = c?.LastOrDefault();

                                                    //insert last candle of precedent period in candles to save
                                                    if (lastPeriodCandle != null)
                                                    {
                                                        var cc = lastPeriodCandle.DeepClone();
                                                        cc.Timestamp = startDate;
                                                        candlesToAdd.Add(cc);
                                                    }
                                                }
                                                else //data on db for period
                                                {
                                                    //understand where is hole
                                                    var innerExpected = expectedCandlesList.DeepClone();
                                                    innerExpected.RemoveAll(i => data2.Select(d => d.Timestamp).Contains(i));

                                                    foreach (var expectedItem in innerExpected)
                                                    {
                                                        bool found = false;
                                                        for (int i = 0; i < data2.Count; i++)
                                                        {
                                                            if (expectedItem == data2[i].Timestamp)
                                                            {
                                                                found = true;
                                                                continue;
                                                            }
                                                        }

                                                        if (!found)
                                                        {
                                                            //find the close to
                                                            Candle newAdd = null;

                                                            long min = long.MaxValue;
                                                            foreach (var dbItem in data2)
                                                            {
                                                                var d = Math.Abs(expectedItem.Subtract(dbItem.Timestamp).Ticks);

                                                                if (d < min)
                                                                {
                                                                    min = d;

                                                                    if (min == 0) //exact date
                                                                        continue;

                                                                    var n = dbItem.DeepClone();
                                                                    n.Timestamp = expectedItem;
                                                                    newAdd = n;
                                                                }
                                                            }

                                                            if (newAdd != null)
                                                                candlesToAdd.Add(newAdd);
                                                        }
                                                    }
                                                }
                                            }

                                            candlesToAdd = candlesToAdd.OrderBy(c => c.Timestamp).ToList();

                                            var newCandlesToAdd2 = new List<Candle>();
                                            //find definitive position
                                            foreach (var excItem in candlesToAdd)
                                            {
                                                Candle newAdd = null;

                                                long min = long.MaxValue;
                                                foreach (var expectedItem in expectedCandlesList)
                                                {
                                                    var d = Math.Abs(expectedItem.Subtract(excItem.Timestamp).Ticks);

                                                    if (d < min)
                                                    {
                                                        min = d;

                                                        var n = excItem.DeepClone();
                                                        n.Timestamp = expectedItem;
                                                        newAdd = n;
                                                    }
                                                }

                                                newCandlesToAdd2.Add(newAdd);
                                            }

                                            //fill holes
                                            foreach (var expectedItem in expectedCandlesList)
                                            {
                                                Candle newAdd = null;

                                                long min = long.MaxValue;
                                                foreach (var dbItem in data2)
                                                {
                                                    var d = Math.Abs(expectedItem.Subtract(dbItem.Timestamp).Ticks);

                                                    if (d < min)
                                                    {
                                                        min = d;

                                                        if (d == 0) //exact date
                                                            continue;

                                                        var n = dbItem.DeepClone();
                                                        n.Timestamp = expectedItem;
                                                        newAdd = n;
                                                    }
                                                }

                                                //if distance is less then period (e.g. 15min) and is not exact date
                                                if (min > 0 && TimeSpan.FromTicks(min).TotalMinutes <= (int)backtestOptions.CandlePeriod.FromMinutesEquivalent() / 2)
                                                {
                                                    newCandlesToAdd2.Add(newAdd);
                                                }
                                                else if (TimeSpan.FromTicks(min).TotalMinutes > (int)backtestOptions.CandlePeriod.FromMinutesEquivalent() / 2)//not found equivalence: clone
                                                {
                                                    var cc = candlesToAdd.Last().DeepClone();
                                                    cc.Timestamp = expectedItem;
                                                    newCandlesToAdd2.Add(cc);
                                                }
                                            }



                                            await dataStore.SaveBacktestCandlesBulkCheckExisting(newCandlesToAdd2, backtestOptions);

                                            callback($"\tUpdated cloning\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                                      $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                                      $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC ");

                                            startDate = backtestOptions.EndDate;
                                        }
                                        else //got right num of candles
                                        {
                                            await dataStore.SaveBacktestCandlesBulkCheckExisting(candlesToAdd, backtestOptions);

                                            callback($"\tUpdated from Exchange\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                                     $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                                     $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC ");

                                            startDate = candlesToAdd.Last().Timestamp.ToUniversalTime();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        callback($"\tError while updating from\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                                 $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                                 $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC ");

                                        startDate = backtestOptions.EndDate;

                                        continue;
                                    }
                                }
                                else if (numOfCandles2 > numOfExpectedCandles2) //multiple
                                {
                                    callback($"\tFound multiple\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                             $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                             $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC " +
                                             $"expected:{numOfExpectedCandles2}, have:{numOfCandles2} ");

                                    foreach (var dbItem in data2)
                                    {
                                        callback($"\t dbItem: {dbItem.Timestamp} {dbItem.Close}");
                                    }

                                    await dataStore.DeleteBacktestCandles(backtestOptions);

                                    var expectedCandlesList2 = new List<DateTime>();

                                    for (int i = 0; i < numOfExpectedCandles2; i++)
                                    {
                                        var t = backtestOptions.StartDate.AddMinutes(i * (int)backtestOptions.CandlePeriod.FromMinutesEquivalent());
                                        expectedCandlesList2.Add(t);
                                    }

                                    List<Candle> candlesToAdd = await baseExchangeApi.GetTickerHistory(exchangeSymbol,
                                                                                                       backtestOptions.CandlePeriod.FromMinutesEquivalent(),
                                                                                                       backtestOptions.StartDate,
                                                                                                       backtestOptions.EndDate);

                                    var newCandlesToAdd2 = new List<Candle>();
                                    foreach (var expectedItem in expectedCandlesList2)
                                    {
                                        Candle newAdd = null;

                                        long min = long.MaxValue;
                                        foreach (var excItem in candlesToAdd)
                                        {
                                            var d = Math.Abs(expectedItem.Subtract(excItem.Timestamp).Ticks);

                                            if (d < min)
                                            {
                                                min = d;

                                                var n = excItem.DeepClone();
                                                n.Timestamp = expectedItem;
                                                newAdd = n;
                                            }
                                        }

                                        //if distance is less then period (e.g. 15min) 
                                        if (newAdd != null && TimeSpan.FromTicks(min).TotalMinutes <= (int)backtestOptions.CandlePeriod.FromMinutesEquivalent() / 2)
                                            newCandlesToAdd2.Add(newAdd);
                                    }

                                    await dataStore.SaveBacktestCandlesBulkCheckExisting(newCandlesToAdd2, backtestOptions);

                                    var data3 = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);
                                    foreach (var dbItem in data3)
                                    {
                                        callback($"\t dbItem: {dbItem.Timestamp} {dbItem.Close}");
                                    }

                                    startDate = newCandlesToAdd.Last().Timestamp.ToUniversalTime();
                                }
                            }
                            else
                            {
                                callback($"\tNo update\t: {backtestOptions.Exchange.ToString()} on {globalSymbol} " +
                                         $"with Period {backtestOptions.CandlePeriod.ToString()}min " +
                                         $"\tfrom {backtestOptions.StartDate.ToUniversalTime()} \tto {backtestOptions.EndDate.ToUniversalTime()} UTC ");

                                startDate = backtestOptions.EndDate;
                            }
                        }
                    }
                }

                lock (CurrentlyRunningUpdates)
                {
                    CurrentlyRunningUpdates.Remove(currentlyRunningString);
                }
            }
        }

        public static async Task<JArray> GetCacheAge(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            var jArrayResult = new JArray();

            foreach (var globalSymbol in backtestOptions.Coins)
            {
                backtestOptions.Coin = globalSymbol;

                //Candle currentHistoricalDataFirst = await dataStore.GetBacktestFirstCandle(backtestOptions);
                //Candle currentHistoricalDataLast = await dataStore.GetBacktestLastCandle(backtestOptions);

#warning //TODO create a Count method

                var data = await dataStore.GetBacktestCandlesBetweenTime(backtestOptions);
                Candle currentHistoricalDataFirst = data.FirstOrDefault();
                Candle currentHistoricalDataLast = data.LastOrDefault();

                if (currentHistoricalDataFirst != null && currentHistoricalDataLast != null)
                {
                    var dataFirst = currentHistoricalDataFirst.Timestamp.ToUniversalTime();
                    var dataLast = currentHistoricalDataLast.Timestamp.ToUniversalTime();
                    var minutes = dataLast.Subtract(dataFirst).TotalMinutes;
                    var numOfCandles = data.Count();
                    var numOfExpectedCandles = minutes / backtestOptions.CandlePeriod;

                    var currentResult = new JObject();
                    currentResult["Exchange"] = backtestOptions.Exchange.ToString();
                    currentResult["Coin"] = globalSymbol;
                    currentResult["CandlePeriod"] = backtestOptions.CandlePeriod;
                    currentResult["FirstCandleDate"] = dataFirst;
                    currentResult["LastCandleDate"] = dataLast;
                    currentResult["CandlesNum"] = numOfCandles;
                    currentResult["ExpectedCandlesNum"] = numOfExpectedCandles;
                    jArrayResult.Add(currentResult);
                }
            }
            return jArrayResult;
        }

        public static void GetCacheAgeConsole(BacktestOptions backtestOptions, IDataStoreBacktest dataStore)
        {
            Console.WriteLine("\tBacktest StartDate: " + Convert.ToDateTime(backtestOptions.StartDate).ToUniversalTime() + " UTC");
            if (backtestOptions.EndDate != DateTime.MinValue)
            {
                Console.WriteLine("\tBacktest EndDate: " + Convert.ToDateTime(backtestOptions.EndDate).ToUniversalTime() + " UTC");
            }
            else
            {
                Console.WriteLine("\tBacktest EndDate: " + DateTime.UtcNow + " UTC");
            }

            Console.WriteLine("");

            int dataCount = 0;
            foreach (var globalSymbol in backtestOptions.Coins)
            {
                backtestOptions.Coin = globalSymbol;

                Candle currentHistoricalDataFirst = dataStore.GetBacktestFirstCandle(backtestOptions).Result;
                Candle currentHistoricalDataLast = dataStore.GetBacktestLastCandle(backtestOptions).Result;
                if (currentHistoricalDataFirst != null && currentHistoricalDataLast != null)
                {
                    Console.WriteLine("\tAvailable Cache for " + backtestOptions.Exchange + " " + globalSymbol + " Period: " + backtestOptions.CandlePeriod + "min  - from " + currentHistoricalDataFirst.Timestamp.ToUniversalTime() + " until " + currentHistoricalDataLast.Timestamp.ToUniversalTime());
                    dataCount = dataCount + 1;
                }

            }

            if (dataCount == 0)
            {
                Console.WriteLine("\tNo data - Please run 4. Refresh candle data first");
            }

            Console.WriteLine();
        }
    }
}
