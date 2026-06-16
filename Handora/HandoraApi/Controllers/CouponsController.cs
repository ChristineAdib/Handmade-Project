using HandoraApplication.DTOs.CouponDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController(ICouponService couponService) : ControllerBase
    {
        private readonly ICouponService _couponService = couponService;
        private string CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [Authorize(Roles = AppRoles.Seller)]
        [HttpPost]
        public async Task<IActionResult> CreateCoupon(CreateCouponDto dto)
        {
            var result = await _couponService.CreateCouponAsync(CurrentUserId, dto);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetMyCoupons), null, result.Data)
                : BadRequest(new { errors = result.Errors });
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCoupon(Guid id, UpdateCouponDto dto)
        {
            var result = await _couponService.UpdateCouponAsync(CurrentUserId, id, dto);
            if (!result.IsSuccess)
            {
                if (result.Errors?.Contains("Unauthorized") == true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { errors = result.Errors });
                }
                return BadRequest(new { errors = result.Errors });
            }
            return Ok(result.Data);
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCoupon(Guid id)
        {
            var result = await _couponService.DeleteCouponAsync(CurrentUserId, id);
            if (!result.IsSuccess)
            {
                if (result.Errors?.Contains("Unauthorized") == true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { errors = result.Errors });
                }
                return BadRequest(new { errors = result.Errors });
            }
            return NoContent();
        }

        [Authorize(Roles = AppRoles.Seller)]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCoupons()
        {
            var result = await _couponService.GetMyCouponsAsync(CurrentUserId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyCoupon(ApplyCouponDto dto)
        {
            var result = await _couponService.ApplyCouponAsync(dto);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { errors = result.Errors });
        }
    }
}
