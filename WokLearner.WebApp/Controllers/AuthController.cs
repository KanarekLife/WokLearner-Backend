using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;

namespace WokLearner.WebApp.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<AppUser> _userManager;

        public AuthController(JwtSettings jwtSettings, UserManager<AppUser> userManager)
        {
            _jwtSettings = jwtSettings;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> GenerateToken([FromBody] AccountModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null) return Unauthorized();

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await _userManager.AccessFailedAsync(user);
                return Unauthorized();
            }

            var symmetricKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role,
                        await _userManager.IsInRoleAsync(user, "Administrator") ? "Administrator" : "User"),
                    new Claim(ClaimTypes.GivenName, model.Username) 
                }),
                Audience = _jwtSettings.Audience,
                Issuer = _jwtSettings.Issuer,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddSeconds(_jwtSettings.ExpireTimeInSeconds),
                SigningCredentials = signingCredentials
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}