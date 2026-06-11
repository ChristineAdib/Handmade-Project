using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService, IDistributedCache cache) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly IDistributedCache _cache = cache;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            return Unauthorized();

        var result = await _orderService.CreateOrder(userId, email, dto);
        if (result.IsSuccess)
        {
            // Clear memory/Redis cart cache key
            var cartKey = $"cart:user:{userId}";
            await _cache.RemoveAsync(cartKey);

            return CreatedAtAction(nameof(GetOrder), new { id = result.Data!.Id }, result.Data);
        }
        return BadRequest(result.Errors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await _orderService.GetOrderById(id, userId, isAdmin);
        if (result.IsSuccess)
            return Ok(result.Data);
        return NotFound(result.Errors);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryDto query)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.GetUserOrders(userId, query);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await _orderService.UpdateOrderStatus(id, dto, userId, isAdmin);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.CancelOrder(id, userId);
        if (result.IsSuccess)
            return Ok(new { message = "Order cancelled successfully" });
        return BadRequest(result.Errors);
    }

    [HttpGet("seller/{shopId}")]
    [Authorize(Roles = AppRoles.Seller)]
    public async Task<IActionResult> GetSellerOrders(Guid shopId, [FromQuery] OrderQueryDto query)
    {
        var result = await _orderService.GetSellerOrders(shopId, query);
        if (result.IsSuccess)
            return Ok(result.Data);
        return BadRequest(result.Errors);
    }

    [HttpGet("test-detail/{id:guid}")]
    public async Task<IActionResult> TestGetOrderDetail(Guid id, [FromQuery] string userId)
    {
        try
        {
            var result = await _orderService.GetOrderById(id, userId, false);
            var logPath = @"C:\Users\EG.LAP\.gemini\antigravity\brain\5349933c-d2e2-4030-b790-5118274db606\scratch\api_test.log";
            System.IO.File.WriteAllText(logPath, $"Success: {result.IsSuccess}. Errors: {string.Join(", ", result.Errors ?? new string[0])}. Data null: {result.Data == null}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            var logPath = @"C:\Users\EG.LAP\.gemini\antigravity\brain\5349933c-d2e2-4030-b790-5118274db606\scratch\api_test.log";
            System.IO.File.WriteAllText(logPath, $"Exception: {ex.Message}\nStack: {ex.StackTrace}");
            throw;
        }
    }
}
