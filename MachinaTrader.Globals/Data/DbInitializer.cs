using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB.Data;
using AspNetCore.Identity.LiteDB.Models;
using Microsoft.AspNetCore.Identity;
using IdentityRole = AspNetCore.Identity.LiteDB.IdentityRole;


namespace MachinaTrader.Globals.Data
{

    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ILiteDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseInitializer(
            ILiteDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task Initialize()
        {
            //Create the Administartor Role
            var resultRoleManager = _roleManager.CreateAsync(new IdentityRole("Administrator")).Result;

            //Create the default Admin account and apply the Administrator role
            string userName = Global.Configuration.SystemOptions.DefaultUserName;
            string userEmail = Global.Configuration.SystemOptions.DefaultUserEmail;
            string userPassword = Global.Configuration.SystemOptions.DefaultUserPassword;

            var user = _userManager.FindByNameAsync(userEmail).Result;

            if (user == null)
            {
                //Like on registration UserName is userEmail
                var userCreated = new ApplicationUser { UserName = userEmail, Email = userEmail, EmailConfirmed = true, AccountEnabled = true };
                var resultCreateAsync = _userManager.CreateAsync(userCreated, userPassword).Result;
                var resultAddToRoleAsync = _userManager.AddToRoleAsync(_userManager.FindByEmailAsync(userEmail).Result, "Administrator").Result;
            }
            else
            {
                var resultDeletePassword = _userManager.RemovePasswordAsync(user).Result;
                var resultResetPassword = _userManager.AddPasswordAsync(user, userPassword).Result;
            }
        }
    }

}
