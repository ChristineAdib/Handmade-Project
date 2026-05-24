using HandoraApplication.DTOs.ShopDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopsController(IShopService shopService) : ControllerBase
    {
        private readonly IShopService _shopService = shopService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetShop(Guid id)
        {
            var result = await _shopService.GetShopById(id);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Errors);
        }

        [HttpGet("{id:guid}/products")]
        public async Task<IActionResult> GetShopWithProducts(Guid id)
        {
            var result = await _shopService.GetShopWithProducts(id);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Errors);
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopRated([FromQuery] int count = 10)
        {
            var result = await _shopService.GetTopRatedShops(count);
            return Ok(result.Data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchShops([FromQuery] ShopFilterDto filter)
        {
            var result = await _shopService.SearchShops(filter);
            return Ok(result.Data);
        }

        [Authorize]
        [HttpGet("my-shop")]
        public async Task<IActionResult> GetMyShop()
        {
            var result = await _shopService.GetMyShop(CurrentUserId);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Errors);
        }

        [Authorize]
        [HttpGet("my-shop/stats")]
        public async Task<IActionResult> GetMyStats()
        {
            var shopResult = await _shopService.GetMyShop(CurrentUserId);
            if (!shopResult.IsSuccess) return NotFound(shopResult.Errors);

            var result = await _shopService.GetShopStats(shopResult.Data!.Id);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateShop(CreateShopDto dto)
        {
            var result = await _shopService.CreateShop(CurrentUserId, dto);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetShop), new { id = result.Data!.Id }, result.Data)
                : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateShop(Guid id, UpdateShopDto dto)
        {
            var result = await _shopService.UpdateShop(id, CurrentUserId, dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteShop(Guid id)
        {
            var result = await _shopService.DeleteShop(id, CurrentUserId);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }
    }
}