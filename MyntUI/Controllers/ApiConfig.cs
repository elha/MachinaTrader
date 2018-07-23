using Microsoft.AspNetCore.Mvc;
using MyntUI.Helpers;
using MyntUI.Models;
using MyntUI.TradeManagers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyntUI.Controllers
{
    [Route("api/core/config/mainconfig")]
    public class ApiCoreConfigMainconfig : Controller
    {
        [HttpGet]
        public ActionResult Get()
        {
            string jsonToFile = JsonConvert.SerializeObject(Globals.Configuration, Formatting.Indented);
            //Console.WriteLine(jsonToFile); // single line JSON string
            return new JsonResult(Globals.Configuration);
        }

        [HttpPost]
        public void Post([FromBody]JObject data)
        {
            //string message = string.Format("Json: '{0}'", data);
            //Console.WriteLine(message);
            try
            {
                Globals.Configuration = MergeObjects.MergeCsDictionaryAndSave(Globals.Configuration, "MainConfig.json", data).ToObject<MainConfig>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Can not save config file: " + ex);
            }
        }
    }

}
