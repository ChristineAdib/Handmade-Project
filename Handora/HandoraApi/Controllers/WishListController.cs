using HandoraApplication.DTOs.WishlistDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishListController(IWishListService wishListService) : ControllerBase
    {
        private readonly IWishListService _wishListService = wishListService;

        // private string GetUserId() =>
        //User.FindFirstValue(ClaimTypes.NameIdentifier)!;


        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // مؤقت للـ debugging
            Console.WriteLine($"=== UserId from Token: '{userId}' ===");
            Console.WriteLine($"=== IsAuthenticated: {User.Identity?.IsAuthenticated} ===");
            return userId!;
        }
        // GET api/wishlist
        // GET api/wishlist
        [HttpGet]
        public async Task<IActionResult> GetWishList()
        {
            var result = await _wishListService.GetWishListAsync(GetUserId());
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToWishListDto dto)
        {
            var result = await _wishListService.AddItemAsync(GetUserId(), dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { success = false, message = result.Errors?.FirstOrDefault() });
        }

        // DELETE api/wishlist/{productId}
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var result = await _wishListService.RemoveItemAsync(GetUserId(), productId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpPost("sync")]
        [Authorize]
        public async Task<IActionResult> SyncGuestWishList([FromBody] List<Guid> productIds)
        {
            var result = await _wishListService.SyncWishListAsync(GetUserId(), productIds);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }
    }
}
