using System.Collections.Generic;
using AspNetCore.Identity.MongoDbCore.Models;

namespace WokLearner.WebApp.Entities
{
    public class AppUser : MongoIdentityUser
    {
        public Dictionary<string, int> LearningStatus { get; set; } = new Dictionary<string, int>();
        public int SkipLevel { get; set; } = 3;
    }
}