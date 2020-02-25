using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;

namespace WokLearner.WebApp.Utils
{
    public static class DatabaseSeed
    {
        public static async Task CreateDefaultAdmin(DefaultAdminSettings account, UserManager<AppUser> userManager,
            RoleManager<MongoIdentityRole> roleManager)
        {
            var user = new AppUser
            {
                UserName = account.Username
            };
            if (!await roleManager.RoleExistsAsync("Administrator"))
                await roleManager.CreateAsync(new MongoIdentityRole("Administrator"));
            if (!(await userManager.GetUsersInRoleAsync("Administrator")).Any())
            {
                await userManager.CreateAsync(user, account.Password);
                await userManager.AddToRoleAsync(user, "Administrator");
            }
        }
    }
}