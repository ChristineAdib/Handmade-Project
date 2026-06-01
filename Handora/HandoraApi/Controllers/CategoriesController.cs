using HandoraApplication.IServices;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    // GET /api/categories
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await _categoryService.GetAllCategories();
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    // GET /api/categories/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var result = await _categoryService.GetCategoryById(id);
        if (result.IsSuccess)
            return Ok(result.Data);
        return NotFound(result.Errors);
    }

    // GET /api/categories/{parentId}/subcategories
    [HttpGet("{parentId}/subcategories")]
    public async Task<IActionResult> GetSubCategories(Guid parentId)
    {
        var result = await _categoryService.GetSubCategories(parentId);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }
}
