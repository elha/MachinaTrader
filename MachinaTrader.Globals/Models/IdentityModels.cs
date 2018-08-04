using Microsoft.AspNetCore.Identity;

namespace MachinaTrader.Globals.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool AccountEnabled { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

}
