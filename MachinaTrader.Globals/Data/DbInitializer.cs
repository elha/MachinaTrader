using System;
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
            try
            {
                //Create the Administartor Role
                string administratorRoleName = "Administrator";
                bool adminRoleExists = _roleManager.RoleExistsAsync("Administrator").Result;
                if (!adminRoleExists)
                {
                    Global.Logger.Information("Adding Administrator role");
                    var resultRoleManager = _roleManager.CreateAsync(new IdentityRole()
                    {
                        Name = administratorRoleName,
                        NormalizedName = administratorRoleName.ToUpper()
                    }).Result;
                }

                //Create the default Admin account and apply the Administrator role
                string userName = Global.Configuration.SystemOptions.DefaultUserName;
                string userEmail = Global.Configuration.SystemOptions.DefaultUserEmail;
                string userPassword = Global.Configuration.SystemOptions.DefaultUserPassword;

                ApplicationUser user = _userManager.FindByEmailAsync(userEmail).Result;

                if (user == null)
                {
                    Global.Logger.Information("Adding user " + userEmail);
                    //Like on registration UserName is userEmail
                    var userCreated = new ApplicationUser
                    {
                        UserName = userEmail,
                        Email = userEmail,
                        EmailConfirmed = true,
                        AccountEnabled = true
                    };
                    var resultCreateAsync = _userManager.CreateAsync(userCreated, userPassword).Result;
                    var resultAddToRoleAsync = _userManager.AddToRoleAsync(userCreated, administratorRoleName.ToUpper()).Result;
                }
                else
                {
                    var resultDeletePassword = _userManager.RemovePasswordAsync(user).Result;
                    var resultResetPassword = _userManager.AddPasswordAsync(user, userPassword).Result;
                }
            }
            catch (Exception e)
            {
                Global.Logger.Error("Cant update admin user - Error: " + e);
            }
        }
    }
}
