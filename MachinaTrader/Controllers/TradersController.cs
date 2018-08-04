using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mynt.Core.Models;

namespace MachinaTrader.Controllers
{
    [Route("api/[controller]")]
    public class TradersController : Controller
    {
        [HttpGet]
        public async Task<List<Trader>> Get()
        {
            return await Runtime.GlobalDataStore.GetTradersAsync();
        }
    }
}
