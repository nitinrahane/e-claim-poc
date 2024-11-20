using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDirectory;

    public LocalFileStorageService()
    {
        _storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        // Generate unique file name to avoid conflicts
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_storageDirectory, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }
}
