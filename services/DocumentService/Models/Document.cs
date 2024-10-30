// Models/Document.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocumentService.Models
{
    public class Document
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonIgnore] // Ignore this field in MongoDB, only use for JSON serialization
        public string IdString => Id.ToString(); // Serialize Id as a string for API responses


        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
