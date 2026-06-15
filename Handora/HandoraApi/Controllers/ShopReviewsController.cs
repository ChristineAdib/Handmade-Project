using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ShopReviewDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopReviewsController(IShopReviewService shopReviewService) : ControllerBase
    {
        private readonly IShopReviewService _shopReviewService = shopReviewService;

        // GET /api/shopreviews/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetReviewById(Guid id)
        {
            var result = await _shopReviewService.GetReviewById(id);
            if (result.IsSuccess)
                return Ok(result.Data);
            return NotFound(result.Errors);
        }

        // GET /api/shopreviews/shop/{shopId}
        [HttpGet("shop/{shopId:guid}")]
        public async Task<IActionResult> GetShopReviews(Guid shopId, [FromQuery] PaginationQueryDto query)
        {
            var result = await _shopReviewService.GetShopReviews(shopId, query);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(result.Errors);
        }

        // GET /api/shopreviews/shop/{shopId}/myReview
        [HttpGet("shop/{shopId:guid}/myReview")]
        [Authorize]
        public async Task<IActionResult> GetUserReviewForShop(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return Unauthorized();

            var result = await _shopReviewService.GetUserReviewForShop(shopId, userId);
            if (result.IsSuccess)
                return Ok(result.Data);
            return NotFound(result.Errors);
        }

        // POST /api/shopreviews
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateShopReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return Unauthorized();

            var result = await _shopReviewService.CreateReview(dto, userId);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Validation failed." });
        }

        // PUT /api/shopreviews/{id}
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateShopReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return Unauthorized();

            var result = await _shopReviewService.UpdateReview(id, dto, userId);
            if (result.IsSuccess)
                return Ok(result.Data);
            return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Update failed." });
        }

        // DELETE /api/shopreviews/{id}
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return Unauthorized();

            var result = await _shopReviewService.DeleteReview(id, userId);
            if (result.IsSuccess)
                return Ok(new { message = "Review deleted successfully" });
            return BadRequest(result.Errors);
        }
    }
}
