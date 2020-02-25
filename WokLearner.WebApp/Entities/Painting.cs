using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WokLearner.WebApp.Entities
{
    public class Painting
    {
        [BsonId]
        [BsonElement("id")]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        [BsonElement("author")] public string Author { get; set; }

        [BsonElement("style")] public string Style { get; set; }

        [BsonElement("filename")] public string FileName { get; set; }
    }
}