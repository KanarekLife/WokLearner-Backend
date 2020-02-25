using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;

namespace WokLearner.WebApp.Controllers
{
    [ApiController]
    [Route("account")]
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public AccountsController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] AccountModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.Username))
                return Problem("Couldn't create the user! There is no username or password", "", 400,
                    "User creation problem!");
            var user = new AppUser
            {
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
                return Ok();
            return Problem("Couldn't create the user! Verify password complexity!", "", 400,
                "User creation problem!");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> Remove()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            if ((await _userManager.DeleteAsync(user)).Succeeded)
                return Ok();
            return Problem("Couldn't remove the user! Contact the administrator", "", 400, "User removal problem!");
        }

        [HttpDelete("admin/remove")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Remove(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Problem("Couldn't find the user with given id.", "", 400, "User not found!");
            if ((await _userManager.DeleteAsync(user)).Succeeded)
                return Ok();
            return Problem("Couldn't remove the user! Try removing directly in the database!", "", 400,
                "User removal problem!");
        }

        [HttpPut("change-username")]
        public async Task<IActionResult> ChangeUsername(string newUsername)
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            if ((await _userManager.SetUserNameAsync(user, newUsername)).Succeeded)
                return Ok();
            return Problem("Couldn't change your username.", "", 400, "Username change problem.");
        }

        [HttpPut("admin/change-username")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ChangeUsername(string id, string newUsername)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Problem("Couldn't find the user with given id.", "", 400, "Username change problem.");
            if ((await _userManager.SetUserNameAsync(user, newUsername)).Succeeded)
                return Ok();
            return Problem("Couldn't change the username!", "", 400, "Username change problem.");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody]PasswordChangeModel model)
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            if (model.NewPassword != model.RepeatedNewPassword)
            {
                return Problem("Couldn't change your password!", "", 400, "Password change problem.");
            }
            if ((await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword)).Succeeded)
                return Ok();
            return Problem("Couldn't change your password!", "", 400, "Password change problem.");
        }

        [HttpGet("admin/list-users")]
        [Authorize(Roles = "Administrator")]
        public IActionResult GetAll()
        {
            return Json(_userManager.Users);
        }
    }
}