using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController(ITagService tagService) : ControllerBase
{
    private readonly ITagService _tagService = tagService;

    // GET /api/tags
    [HttpGet]
    public async Task<IActionResult> GetAllTags()
    {
        var result = await _tagService.GetAllTags();
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    // GET /api/tags/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTagById(Guid id)
    {
        var result = await _tagService.GetTagById(id);
        if (result.IsSuccess)
            return Ok(result.Data);
        return NotFound(result.Errors);
    }
}
