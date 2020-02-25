using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;
using WokLearner.WebApp.Utils;

namespace WokLearner.WebApp.Controllers
{
    [ApiController]
    [Authorize]
    [Route("learning")]
    public class LearningController : Controller
    {
        private readonly IMongoCollection<Painting> _paintingsCollection;
        private readonly UserManager<AppUser> _userManager;

        public LearningController(UserManager<AppUser> userManager, IMongoClient mongoClient,
            DatabaseSettings databaseSettings)
        {
            _userManager = userManager;
            _paintingsCollection = _paintingsCollection = mongoClient.GetDatabase(databaseSettings.PaintingsDatabase)
                .GetCollection<Painting>("paintings");
        }

        [HttpPost("clear-learned")]
        public async Task<IActionResult> ClearLearned()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            user.LearningStatus = new Dictionary<string, int>();
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("answer")]
        public async Task<IActionResult> Answer(string paintingId)
        {
            var guidParseResult = Guid.TryParse(paintingId, out var guid);
            if (!guidParseResult) return Problem("Given id looks wrong!", "", 400, "Painting issue.");
            var result = _paintingsCollection.Find(x => x.Id == guid);
            if (await result.CountDocumentsAsync() == 1)
            {
                var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
                if(!user.LearningStatus.ContainsKey(paintingId))
                {
                    user.LearningStatus.Add(paintingId,0);
                }
                user.LearningStatus[paintingId]++;
                await _userManager.UpdateAsync(user);
                return Ok();
            } 
            if (await result.CountDocumentsAsync() == 0)
                return Problem("Couldn't find painting with given id.", "", 400, "Painting issue.");
            return Problem("Too many paintings with same id. Contact the administrator!", "", 400,
                "Painting issue.");
        }

        [HttpGet("get-guesses/{paintingId}")]
        public async Task<IActionResult> GetGuesses(string paintingId)
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            if (!user.LearningStatus.ContainsKey(paintingId))
            {
                user.LearningStatus.Add(paintingId, 0);
                await _userManager.UpdateAsync(user);
            }
            
            return Ok(user.LearningStatus[paintingId]);
        }

        [HttpGet("get-guesses")]
        public async Task<IActionResult> GetGuesses()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            return Json(user.LearningStatus.Count(x => x.Value>=user.SkipLevel));
        }

        [HttpGet("skip-level")]
        public async Task<IActionResult> SkipLevel()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            return Ok(user.SkipLevel);
        }

        [HttpPost("skip-level")]
        public async Task<IActionResult> SkipLevel(int skipLevel)
        {
            if (skipLevel < 0)
                return Problem("Skip level cannot be set under 1.", "", 400, "Skip level setup problem.");
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            user.SkipLevel = skipLevel;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        //Difference to normal get is ability to skip already learned paintings
        [HttpGet("learn")]
        public async Task<IActionResult> Learn()
        {
            var user = await _userManager.FindByIdAsync(HttpContext.User.Identity.Name);
            var count = await _paintingsCollection.CountDocumentsAsync(x => true);
            if (user.LearningStatus.Any(x => x.Value < user.SkipLevel) || count > user.LearningStatus.Count())
                while (true)
                {
                    var painting = (await _paintingsCollection.Find(x => true).ToListAsync()).Randomize().First();
                    if (!user.LearningStatus.ContainsKey(painting.Id.ToString()) ||
                        user.LearningStatus[painting.Id.ToString()] < user.SkipLevel)
                        return Ok(painting);
                }

            return Problem("Congrats! You have already learned everything! Try removing the progress.", "", 400,
                "Learned everything.");

            ;
        }

        private string NormalizeAnswer(string input)
        {
            return new string(input.Trim().ToUpper().Normalize().Replace(" ", "").Where(x => x > 40 && x < 91).ToArray());
        }
    }
}