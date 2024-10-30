using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using DocumentService.Models;
using System.Collections.Generic;

namespace DocumentService.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly IMongoCollection<Document> _collection;

        public DocumentRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<Document>("Documents");
        }

        // Get all documents
        public async Task<List<Document>> GetDocumentsAsync()
        {
            return await _collection.Find(new BsonDocument()).ToListAsync();
        }

        // Get a single document by ID
        public async Task<Document> GetDocumentAsync(string id)
        {
            var filter = Builders<Document>.Filter.Eq(d => d.Id, ObjectId.Parse(id));
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // Create a new document
        public async Task CreateDocumentAsync(Document document)
        {
            await _collection.InsertOneAsync(document);
        }

        // Update a document by ID
        public async Task UpdateDocumentAsync(string id, Document document)
        {
            var filter = Builders<Document>.Filter.Eq(d => d.Id, ObjectId.Parse(id));
            await _collection.ReplaceOneAsync(filter, document);
        }

        // Delete a document by ID
        public async Task DeleteDocumentAsync(string id)
        {
            var filter = Builders<Document>.Filter.Eq(d => d.Id, ObjectId.Parse(id));
            await _collection.DeleteOneAsync(filter);
        }
    }
}