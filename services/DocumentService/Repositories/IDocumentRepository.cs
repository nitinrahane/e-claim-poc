// Repositories/IDocumentRepository.cs
using DocumentService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocumentService.Repositories
{
    public interface IDocumentRepository
    {
        Task<List<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentAsync(string id);
        Task CreateDocumentAsync(Document document);
        Task UpdateDocumentAsync(string id, Document document);
        Task DeleteDocumentAsync(string id);
    }
}
