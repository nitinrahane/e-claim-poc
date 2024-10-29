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
                Content = "This is a sample document content.",
                CreatedAt = DateTime.UtcNow
            });
            Console.WriteLine("Seeded initial data to MongoDB.");
        }
    }
}
