// Repositories/DocumentRepository.cs
using DocumentService.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocumentService.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly IMongoCollection<Document> _documents;

        public DocumentRepository(IMongoDatabase database)
        {
            _documents = database.GetCollection<Document>("Documents");
        }

        public async Task<List<Document>> GetDocumentsAsync() =>
            await _documents.Find(doc => true).ToListAsync();

        public async Task<Document> GetDocumentAsync(string id) =>
            await _documents.Find(doc => doc.Id == id).FirstOrDefaultAsync();

        public async Task CreateDocumentAsync(Document document) =>
            await _documents.InsertOneAsync(document);

        public async Task UpdateDocumentAsync(string id, Document document) =>
            await _documents.ReplaceOneAsync(doc => doc.Id == id, document);

        public async Task DeleteDocumentAsync(string id) =>
            await _documents.DeleteOneAsync(doc => doc.Id == id);
    }
}
