using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _productService.GetProduct(id);
        if (result.IsSuccess)
            return Ok(result.Data);
        return NotFound(result.Errors);
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto query)
    {
        var result = await _productService.GetProducts(query);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
    {
        var result = await _productService.CreateProduct(dto);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetProduct), new { id = result.Data!.Id }, result.Data);
        return BadRequest(result.Errors);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromForm] UpdateProductDto dto)
    {
        var result = await _productService.UpdateProduct(id, dto);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _productService.DeleteProduct(id);
        if (result.IsSuccess)
            return NoContent();
        return NotFound(result.Errors);
    }
}
