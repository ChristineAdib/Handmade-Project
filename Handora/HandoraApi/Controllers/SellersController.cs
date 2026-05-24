using HandoraApplication.DTOs.SellerDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController(ISellerService sellerService) : ControllerBase
    {
        private readonly ISellerService _sellerService = sellerService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("{sellerId}")]
        public async Task<IActionResult> GetSellerProfile(string sellerId)
        {
            var result = await _sellerService.GetSellerProfile(sellerId);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Errors);
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpGet("MyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _sellerService.GetMyProfile(CurrentUserId);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Errors);
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpPut("MyProfile")]
        public async Task<IActionResult> UpdateMyProfile(UpdateSellerDto dto)
        {
            var result = await _sellerService.UpdateMyProfile(CurrentUserId, dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }
    }
}