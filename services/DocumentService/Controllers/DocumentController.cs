// Controllers/DocumentController.cs
using DocumentService.Models;
using DocumentService.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace DocumentService.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
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
            await _documentRepository.CreateDocumentAsync(document);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDocument(string id, Document document)
        {
            var existingDocument = await _documentRepository.GetDocumentAsync(id);
            if (existingDocument == null) return NotFound();
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
    }
}
