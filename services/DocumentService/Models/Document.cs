using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocumentService.Models
{
    public class Document
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonIgnore] // Ignore this field in MongoDB, only use for JSON serialization
        public string IdString => Id.ToString();

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("filePath")]
        public string FilePath { get; set; } 

        [BsonElement("contentType")]
        public string ContentType { get; set; } 

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("ownerId")]
        public string OwnerId { get; set; }
    }
}
