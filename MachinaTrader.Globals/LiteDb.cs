using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MachinaTrader.Globals
{
    public class LiteDb
    {
        //LiteDB

        private static Dictionary<string, DbManager> instance = new Dictionary<string, DbManager>();
        public class DbManager
        {
            private LiteDatabase liteDataBase;
            //private static Manager singleInstance;

            private DbManager(string pluginName, string databaseName)
            {
                liteDataBase = new LiteDatabase(Global.DataPath + "/" + pluginName + "/" + databaseName + ".db");
            }

            public static DbManager GetInstance(string pluginName, string databaseName)
            {
                if (!instance.ContainsKey(databaseName))
                {
                    instance["databaseName"] = new DbManager(pluginName, databaseName);
                }
                return instance["databaseName"];
            }

            public LiteCollection<T> GetTable<T>(string collectionName = null) where T : new()
            {
                if (collectionName == null)
                {
                    return liteDataBase.GetCollection<T>(typeof(T).Name);
                }
                return liteDataBase.GetCollection<T>(collectionName);
            }
        }
    }
}
