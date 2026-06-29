using HandoraApplication.AI.Interfaces;
using HandoraApplication.AI.DTOs;
using HandoraApplication.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IRagService _ragService;
        private readonly IProductIndexerService _productIndexerService;

        public AIController(IEmbeddingService embeddingService, IRagService ragService, IProductIndexerService productIndexerService)
        {
            _embeddingService = embeddingService;
            _ragService = ragService;
            _productIndexerService = productIndexerService;
        }

        [HttpPost("embedding")]
        public async Task<IActionResult> GenerateEmbedding([FromBody] string text)
        {
            var embedding = await _embeddingService.GetEmbeddingAsync(text);
            return Ok(embedding);
        }

        [HttpPost("index")]
        public async Task<IActionResult> IndexDocument([FromBody] RagDocumentDto document)
        {
            await _ragService.IndexAsync(document);
            return Ok(new { Message = $"Document {document.Id} indexed successfully." });
        }

        [HttpPost("index-products")]
        public async Task<IActionResult> IndexProducts()
        {
            await _productIndexerService.IndexAllProductsAsync();
            return Ok(ApiResponse<object>.Ok(null!, "All active catalog products have been successfully indexed."));
        }

        [HttpPost("index-artisans")]
        public async Task<IActionResult> IndexArtisans()
        {
            await _productIndexerService.IndexAllArtisansAsync();
            return Ok(ApiResponse<object>.Ok(null!, "All active artisans have been successfully indexed."));
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] RagSearchRequestDto request)
        {
            var results = await _ragService.SearchAsync(request);
            return Ok(results);
        }

        [HttpDelete("document")]
        public async Task<IActionResult> DeleteDocument([FromQuery] string collection, [FromQuery] string id)
        {
            await _ragService.DeleteAsync(collection, id);
            return Ok(new { Message = $"Document {id} deleted successfully from {collection}." });
        }
    }
}

