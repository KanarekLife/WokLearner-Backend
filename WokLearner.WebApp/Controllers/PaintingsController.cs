using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WokLearner.WebApp.Entities;
using WokLearner.WebApp.Models;
using WokLearner.WebApp.Utils;

namespace WokLearner.WebApp.Controllers
{
    [Authorize]
    [Route("paintings")]
    [ApiController]
    public class PaintingsController : Controller
    {
        private readonly IMongoCollection<Painting> _paintingsCollection;
        private readonly string _path;

        public PaintingsController(IMongoClient mongoClient, DatabaseSettings databaseSettings)
        {
            _paintingsCollection = mongoClient.GetDatabase(databaseSettings.PaintingsDatabase)
                .GetCollection<Painting>("paintings");
            _path = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetAll(string author = null, string style = null, bool random = false,
            int size = 20, int page = 0)
        {
            IEnumerable<Painting> result = await _paintingsCollection.Find(x => true).ToListAsync();

            if (author != null) result = result.Where(x => x.Author == author);

            if (style != null) result = result.Where(x => x.Style == style);

            if (random) result = result.Randomize();

            if (size == -1)
                return Json(result);
            return Json(new
            {
                images = result.Skip(size * page).Take(size),
                page,
                pagesCount = result.Count() / size
            });
        }


        [HttpGet("get/styles")]
        public async Task<IActionResult> GetStyles(string author = null)
        {
            IEnumerable<Painting> result = await _paintingsCollection.Find(x => true).ToListAsync();
            if (author != null) result = result.Where(x => x.Author == author);

            return Json(result.Select(x => x.Style).Distinct());
        }

        [HttpGet("get/authors")]
        public async Task<IActionResult> GetAuthors(string style = null)
        {
            IEnumerable<Painting> result = await _paintingsCollection.Find(x => true).ToListAsync();
            if (style != null) result = result.Where(x => x.Style == style);

            return Json(result.Select(x => x.Author).Distinct());
        }

        [HttpGet("get/id/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var guidParseResult = Guid.TryParse(id, out var guid);
            if (!guidParseResult) return Problem("Given id looks wrong!", "", 400, "Painting issue.");
            var result = _paintingsCollection.Find(x => x.Id == guid);
            if (await result.CountDocumentsAsync() == 1)
                return Json(await result.SingleAsync());
            if (await result.CountDocumentsAsync() == 0)
                return Problem("Couldn't find painting with given id.", "", 400, "Painting issue.");
            return Problem("Too many paintings with same id. Contact the administrator!", "", 400,
                "Painting issue.");
        }

        [HttpPost("create")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(bool uploadImage = true)
        {
            if (Request.Form.Files.Count != 1 && uploadImage) return Problem("No paiting file attached!", "", 400, "Painting issue.");
            var painting = new PaintingModel
            {
                Author = Request.Form["author"],
                FileName = Request.Form["filename"],
                Style = Request.Form["style"]
            };
            if (painting.Author == null || painting.Style == null || painting.FileName == null)
                return Problem("Incorrect model structure!", "", 400, "Painting issue.");
            if (uploadImage)
            {
                try
                {
                    var path = Path.Combine(_path, painting.Style, painting.Author);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                
                    var file = Request.Form.Files.First();
                    await using var stream = System.IO.File.Create(Path.Combine(path, painting.FileName));
                    await file.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    return Problem(ex.Message, "", 400, "Paiting issue.");
                }
            }

            var entity = new Painting
            {
                Author = painting.Author,
                Style = painting.Style,
                FileName = painting.FileName
            };
            var searchResult = _paintingsCollection.Find(x =>
                x.Author == entity.Author && x.Style == entity.Style && x.FileName == entity.FileName);
            if (await searchResult.CountDocumentsAsync() == 0)
            {
                await _paintingsCollection.InsertOneAsync(entity);
                return Ok();
            }

            return Problem("Same painting is already in our database!", "", 400, "Painting issue.");
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateSingle(string id, bool replaceImage = false)
        {
            if (replaceImage && !Request.Form.Files.Any())
                return Problem("No painting file attached!", "", 400, "Painting issue.");
            var painting = new PaintingModel
            {
                Author = Request.Form["author"],
                FileName = Request.Form["filename"],
                Style = Request.Form["style"]
            };
            if (painting.Author == null || painting.Style == null || painting.FileName == null)
                return Problem("Incorrect model structure!", "", 400, "Painting issue.");
            var guidParseResult = Guid.TryParse(id, out var guid);
            if (!guidParseResult) return Problem("Given id looks wrong!", "", 400, "Painting issue.");
            var entity = new Painting
            {
                Author = painting.Author,
                Style = painting.Style,
                FileName = painting.FileName,
                Id = guid
            };
            var result = _paintingsCollection.Find(x => x.Id == entity.Id);
            if (await result.CountDocumentsAsync() == 1)
            {
                var current = await result.SingleAsync();
                var oldPath = Path.Combine(_path, current.Style, current.Author, current.FileName);
                var newPath = Path.Combine(_path, entity.Style, entity.Author);
                var newFile = Path.Combine(newPath, entity.FileName);
                if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

                if (replaceImage)
                {
                    await using var fileStream = System.IO.File.Create(newFile);
                    await Request.Form.Files.First().CopyToAsync(fileStream);
                }
                else
                {
                    entity.FileName = current.FileName;
                    System.IO.File.Move(oldPath, newFile);
                }

                System.IO.File.Delete(oldPath);
                await _paintingsCollection.ReplaceOneAsync(x => x.Id == entity.Id, entity);
                return Ok();
            }

            if (await result.CountDocumentsAsync() == 0)
                return Problem("Couldn't find painting with given id.", "", 400, "Painting issue.");
            return Problem("Too many paintings with same id. Contact the administrator!", "", 400,
                "Painting issue.");
        }

        [HttpDelete("remove/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Remove(string id)
        {
            var guidParseResult = Guid.TryParse(id, out var guid);
            if (!guidParseResult) return Problem("Given id looks wrong!", "", 400, "Painting issue.");
            var result = _paintingsCollection.Find(x => x.Id == guid);
            if (await result.CountDocumentsAsync() == 1)
            {
                var current = await result.FirstAsync();
                var path = Path.Combine(_path, current.Style, current.Author);
                System.IO.File.Delete(Path.Combine(_path, path, current.FileName));
                if (!Directory.EnumerateFiles(path).Any()) Directory.Delete(path);
                await _paintingsCollection.DeleteOneAsync(x => x.Id == guid);
                return Ok();
            }

            if (await result.CountDocumentsAsync() == 0)
                return Problem("Couldn't find painting with given id.", "", 400, "Painting issue.");
            return Problem("Too many paintings with same id. Contact the administrator!", "", 400,
                "Painting issue.");
        }
    }
}