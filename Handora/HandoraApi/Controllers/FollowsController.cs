using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowsController(IFollowService followService) : ControllerBase
    {
        private readonly IFollowService _followService = followService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [Authorize]
        [HttpPost("{shopId:guid}")]
        public async Task<IActionResult> FollowShop(Guid shopId)
        {
            var result = await _followService.FollowShop(CurrentUserId, shopId);
            return result.IsSuccess ? Ok() : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpDelete("{shopId:guid}")]
        public async Task<IActionResult> UnfollowShop(Guid shopId)
        {
            var result = await _followService.UnfollowShop(CurrentUserId, shopId);
            return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
        }

        [Authorize]
        [HttpGet("{shopId:guid}/isFollowing")]
        public async Task<IActionResult> IsFollowing(Guid shopId)
        {
            var result = await _followService.IsFollowing(CurrentUserId, shopId);
            return Ok(result.Data);
        }

        [Authorize]
        [HttpGet("myShops")]
        public async Task<IActionResult> GetFollowedShops()
        {
            var result = await _followService.GetFollowedShops(CurrentUserId);
            return Ok(result.Data);
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpGet("{shopId:guid}/followers")]
        public async Task<IActionResult> GetShopFollowers(Guid shopId)
        {
            var result = await _followService.GetShopFollowers(shopId);
            return Ok(result.Data);
        }
    }
}