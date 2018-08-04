using System.Threading.Tasks;
using MachinaTrader.Globals.Models;
using Microsoft.AspNetCore.Identity;

namespace MachinaTrader.Globals.Data
{

    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task Initialize()
        {
            _context.Database.EnsureCreated();

            //Create the Administartor Role
            await _roleManager.CreateAsync(new IdentityRole("Administrator"));

            //Create the default Admin account and apply the Administrator role
            string userName = (string)Global.CoreConfig["coreConfig"]["webDefaultUsername"];
            string userEmail = (string)Global.CoreConfig["coreConfig"]["webDefaultUserEmail"];
            string userPassword = (string)Global.CoreConfig["coreConfig"]["webDefaultPassword"];

            var user = await _userManager.FindByNameAsync(userEmail);
            if (user == null)
            {
                //Like on registration UserName is userEmail
                var userCreated = new ApplicationUser { UserName = userEmail, Email = userEmail, EmailConfirmed = true, AccountEnabled = true};
                var resultCreateAsync = await _userManager.CreateAsync(userCreated, userPassword);
                var resultAddToRoleAsync = await _userManager.AddToRoleAsync(await _userManager.FindByEmailAsync(userEmail), "Administrator");
            }
            else
            {
                var resultDeletePassword = await _userManager.RemovePasswordAsync(user);
                var resultResetPassword = await _userManager.AddPasswordAsync(user, userPassword);
            }

        }
    }
}
