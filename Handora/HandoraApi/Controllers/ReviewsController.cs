using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.ReviewDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    private readonly IReviewService _reviewService = reviewService;

    // GET /api/reviews/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        var result = await _reviewService.GetReviewById(id);
        if (result.IsSuccess)
            return Ok(result.Data);
        return NotFound(result.Errors);
    }

    // GET /api/reviews/product/{productId}
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(Guid productId, [FromQuery] PaginationQueryDto query)
    {
        var result = await _reviewService.GetProductReviews(productId, query);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    // POST /api/reviews
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.CreateReview(dto, userId);
        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Validation failed." });
    }

    // PUT /api/reviews/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.UpdateReview(id, dto, userId);
        if (result.IsSuccess)
            return Ok(result.Data);

        return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Update failed." });
    }

    // DELETE /api/reviews/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.DeleteReview(id, userId);
        if (result.IsSuccess)
            return Ok("Review deleted successfully");
        return BadRequest(result.Errors);
    }

    // GET /api/reviews/my-reviews
    [HttpGet("my-reviews")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.GetUserReviews(userId);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    // GET /api/reviews/eligible/{productId}
    [HttpGet("eligible/{productId}")]
    [Authorize]
    public async Task<IActionResult> CheckEligibility(Guid productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.GetReviewEligibility(productId, userId);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    // POST /api/reviews/demo/{productId}
    [HttpPost("demo/{productId}")]
    [Authorize]
    public async Task<IActionResult> GenerateDemoReviews(Guid productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var result = await _reviewService.GenerateDemoReviewsAsync(productId, userId);
        if (result.IsSuccess)
            return Ok(new { success = true, message = "Demo reviews generated successfully." });

        return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Failed to generate demo reviews." });
    }

    // POST /api/reviews/summarize/{productId}
    [HttpPost("summarize/{productId}")]
    [Authorize]
    public async Task<IActionResult> GenerateAiSummary(Guid productId)
    {
        var result = await _reviewService.GenerateAiSummaryAsync(productId);
        if (result.IsSuccess)
            return Ok(new { success = true, message = "AI review summary generated successfully." });

        return BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() ?? "Failed to generate AI summary." });
    }
}