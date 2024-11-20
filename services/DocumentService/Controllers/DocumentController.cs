using DocumentService.Models;
using DocumentService.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocumentService.Controllers
{
    [Authorize(Policy = "CustomerPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _storageService;

        public DocumentController(IDocumentRepository documentRepository, IFileStorageService storageService)
        {
            _documentRepository = documentRepository;
            _storageService = storageService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Document>>> GetDocuments()
        {
            var documents = await _documentRepository.GetDocumentsAsync();
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(string id)
        {
            var document = await _documentRepository.GetDocumentAsync(id);
            if (document == null) return NotFound();
            return Ok(document);
        }

        [HttpPost]
        public async Task<ActionResult> CreateDocument(Document document)
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepository.CreateDocumentAsync(document);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDocument(string id, Document document)
        {
            var existingDocument = await _documentRepository.GetDocumentAsync(id);
            if (existingDocument == null) return NotFound();

            document.UpdatedAt = DateTime.UtcNow;
            await _documentRepository.UpdateDocumentAsync(id, document);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(string id)
        {
            var document = await _documentRepository.GetDocumentAsync(id);
            if (document == null) return NotFound();
            await _documentRepository.DeleteDocumentAsync(id);
            return NoContent();
        }

        // [ProducesResponseType(StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]        
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                // Save file using LocalFileStorageService
                var filePath = await _storageService.SaveFileAsync(file);

                // Create and store document metadata
                var document = new Document
                {
                    Title = file.FileName,
                    FilePath = filePath,
                    ContentType = file.ContentType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OwnerId = GetUserIdFromJwt()
                };

                await _documentRepository.CreateDocumentAsync(document);

                return Ok(new { Message = "File uploaded successfully", DocumentId = document.IdString });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred", Error = ex.Message });
            }
        }

        private string GetUserIdFromJwt()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "UnknownUser";
        }
    }
}
