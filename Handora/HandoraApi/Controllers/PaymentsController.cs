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
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<PaymentsController> _logger = logger;
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
    [HttpGet("webhook")]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        if (Request.Method == HttpMethod.Get.Method)
        {
            var success = Request.Query["success"].FirstOrDefault() == "true";
            var paymobOrderId = Request.Query["order"].FirstOrDefault();
            var orderGuid = await GetOrderGuidByPaymobId(paymobOrderId);

            if (success && !string.IsNullOrEmpty(paymobOrderId))
            {
                _logger.LogInformation("Webhook GET - capturing payment for order {OrderId}", paymobOrderId);
                await _paymentService.CapturePaymentAsync(paymobOrderId);
            }

            return RedirectWithReplace(success, paymobOrderId, orderGuid);
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var hmacHeader = Request.Headers["X-Paymob-Signature"].FirstOrDefault()
                      ?? Request.Headers["x-paymob-signature"].FirstOrDefault()
                      ?? "";

        _logger.LogInformation("Webhook RAW BODY: {Body}", body);
        _logger.LogInformation("Webhook HMAC: {Hmac}", hmacHeader);

        var result = await _paymentService.VerifyWebhookAsync(body, hmacHeader);

        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("redirect")]
    public async Task<IActionResult> Redirect(
        [FromQuery] bool success,
        [FromQuery] long order)
    {
        var paymobOrderId = order.ToString();
        var orderGuid = await GetOrderGuidByPaymobId(paymobOrderId);

        if (success)
        {
            _logger.LogInformation("Processing redirect for Paymob order {OrderId}", paymobOrderId);
            await _paymentService.CapturePaymentAsync(paymobOrderId);
        }

        return RedirectWithReplace(success, paymobOrderId, orderGuid);
    }

    private async Task<string?> GetOrderGuidByPaymobId(string? paymobOrderId)
    {
        if (string.IsNullOrEmpty(paymobOrderId))
            return null;

        var orderRepo = _unitOfWork.Repository<Order, Guid>();
        var query = await orderRepo.GetAllAsync();
        var order = await query.FirstOrDefaultAsync(o => o.PaymobOrderId == paymobOrderId);
        return order?.Id.ToString();
    }

    private IActionResult RedirectWithReplace(bool success, string? paymobOrderId, string? orderGuid)
    {
        var frontendUrl = _configuration["Paymob:FrontendCallbackUrl"]
                       ?? "http://localhost:4200/payment/callback";

        var redirectUrl = $"{frontendUrl}?success={success.ToString().ToLowerInvariant()}&order={paymobOrderId}&gid={orderGuid}";

        var safeUrl = redirectUrl.Replace("'", "\\'");
        return Content(
            $"<html><body><script>window.location.replace('{safeUrl}')</script></body></html>",
            "text/html");
    }
}
