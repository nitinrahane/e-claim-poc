// DataSeeder.cs
using DocumentService.Models;
using DocumentService.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DataSeeder
{
    private readonly IDocumentRepository _documentRepository;

    public DataSeeder(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task SeedAsync()
    {
        var documents = await _documentRepository.GetDocumentsAsync();

        if (documents.Count == 0)
        {
            await _documentRepository.CreateDocumentAsync(new Document
            {
                Title = "Sample Document",
                FilePath = "/local-storage/documents/sample-document.pdf", // Adjust file path as needed
                ContentType = "application/pdf", // MIME type for the document
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OwnerId = "user12345"
            });
            Console.WriteLine("Seeded initial data to MongoDB.");
        }
    }
}
