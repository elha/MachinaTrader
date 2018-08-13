using System;
namespace Mynt.Data.MongoDB
{
	public class MongoDbOptions
	{
		public string MongoUrl { get; set; } = "mongodb://root:example@mongo:27017";
		public string MongoDatabaseName { get; set; } = "MachinaTrader";
	}
}
