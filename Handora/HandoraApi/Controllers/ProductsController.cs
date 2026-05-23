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

    //[HttpGet]
    //public async Task<IActionResult> GetProducts()
    //{
    //    var result = await _productService.GetProducts();
    //    if (result.IsSuccess)
    //        return Ok(result.Data);
    //    return NotFound(result.Errors);
    //}
}
