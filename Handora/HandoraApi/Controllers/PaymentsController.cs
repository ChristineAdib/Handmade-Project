using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HandoraApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    IPaymentService paymentService,
    IUnitOfWork unitOfWork) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize(Roles = AppRoles.Buyer)]
    [HttpPost("create-intent/{orderId}")]
    public async Task<IActionResult> CreateIntent(Guid orderId)
    {
        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var query = await orderRepo.GetAllAsync();
        var order = await query
            .Include(o => o.User)
            .Include(o => o.DeliveryMethod)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return NotFound("Order not found");

        if (order.UserId != CurrentUserId)
            return BadRequest("You can only pay for your own orders");

        if (order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.Refunded)
            return BadRequest("Order is already paid or refunded");

        var result = await _paymentService.CreatePaymentIntentAsync(order);

        if (!result.IsSuccess)
            return BadRequest(result.Errors);

        return Ok(new { checkoutUrl = result.Data });
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var hmacHeader = Request.Headers["X-Paymob-Signature"].FirstOrDefault()
                      ?? Request.Headers["x-paymob-signature"].FirstOrDefault()
                      ?? "";

        var result = await _paymentService.VerifyWebhookAsync(body, hmacHeader);

        if (!result.IsSuccess)
        {
            // Paymob expects 200 even for invalid signatures to avoid re-delivery
            // Log the failure but return OK
        }

        return Ok();
    }
}
