// Models/Document.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocumentService.Models
{
    public class Document
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
