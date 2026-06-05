using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController(IProfileService profileService) : ControllerBase
    {
        private readonly IProfileService _profileService = profileService;

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _profileService.GetProfileAsync(UserId);
            return Ok(result);
        }

        [HttpGet("followed-shops")]
        public async Task<IActionResult> GetFollowedShops()
        {
            var result = await _profileService.GetFollowedShopsAsync(UserId);
            return Ok(result);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryDto query)
        {
            var result = await _profileService.GetOrdersAsync(UserId, query);
            return Ok(result);
        }
    }
}
