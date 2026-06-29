using HandoraApplication.DTOs.ProductAgentDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HandoraApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductAgentController : ControllerBase
    {
        private readonly IProductAgentService _agentService;

        public ProductAgentController(IProductAgentService agentService)
        {
            _agentService = agentService;
        }

        [HttpPost("analyze-image")]
        public async Task<IActionResult> AnalyzeImage([FromBody] AnalyzeImageRequest request)
        {
            try
            {
                var result = await _agentService.AnalyzeProductImageAsync(
                    request.ImageBase64,
                    request.MimeType
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

   
}
