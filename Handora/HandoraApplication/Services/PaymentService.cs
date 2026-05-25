using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HandoraApplication.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private static readonly HttpClient _httpClient = new();

    public PaymentService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<string>> CreatePaymentIntentAsync(Order order)
    {
        try
        {
            var baseUrl = _configuration["Paymob:BaseUrl"] ?? "https://accept.paymob.com";
            var apiKey = _configuration["Paymob:ApiKey"] ?? "";
            var integrationId = int.Parse(_configuration["Paymob:IntegrationId"] ?? "0");
            var publicKey = _configuration["Paymob:PublicKey"] ?? "";

            // 1. Get auth token
            var token = await GetAuthTokenAsync(baseUrl, apiKey);
            if (string.IsNullOrEmpty(token))
                return Result<string>.Failure("Failed to authenticate with Paymob");

            // 2. Create Paymob order
            var amountCents = (int)(order.SubTotal * 100);
            var paymobOrderId = await CreatePaymobOrderAsync(baseUrl, token, amountCents);
            if (paymobOrderId == null)
                return Result<string>.Failure("Failed to create Paymob order");

            // 3. Get payment key
            var billingData = new
            {
                apartment = "NA",
                email = order.BuyerEmail,
                floor = "NA",
                first_name = order.User?.Name ?? order.BuyerEmail,
                street = "NA",
                building = "NA",
                phone_number = order.User?.PhoneNumber ?? "NA",
                shipping_method = "PKG",
                postal_code = "NA",
                city = "NA",
                country = "EG",
                last_name = ".",
                state = "NA"
            };

            var paymentKey = await GetPaymentKeyAsync(baseUrl, token, amountCents, paymobOrderId.Value, integrationId, billingData);
            if (string.IsNullOrEmpty(paymentKey))
                return Result<string>.Failure("Failed to get payment key from Paymob");

            // 4. Save PaymentIntentId on order
            order.PaymentIntentId = paymobOrderId.Value.ToString();
            var orderRepo = _unitOfWork.Repository<Order, Guid>();
            await orderRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var checkoutUrl = $"{baseUrl}/acceptance/iframes/{integrationId}?payment_token={paymentKey}";

            return Result<string>.Success(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for order {OrderId}", order.Id);
            return Result<string>.Failure($"Payment creation failed: {ex.Message}");
        }
    }

    public async Task<Result> CapturePaymentAsync(string paymentIntentId)
    {
        try
        {
            var orders = _unitOfWork.Repository<Order, Guid>();
            var query = await orders.GetAllAsync();
            var order = await query.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

            if (order == null)
                return Result.Failure("Order not found");

            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Processing;

            await orders.UpdateAsync(order);

            // Create payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.SubTotal,
                Status = PaymentStatus.Paid,
                Provider = "Paymob",
                ProviderOrderId = paymentIntentId,
                PaidAt = DateTime.UtcNow,
                Currency = "EGP"
            };

            var paymentRepo = _unitOfWork.Repository<Payment, Guid>();
            await paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing payment {PaymentIntentId}", paymentIntentId);
            return Result.Failure($"Capture failed: {ex.Message}");
        }
    }

    public async Task<Result> VerifyWebhookAsync(string requestBody, string hmacHeader)
    {
        try
        {
            var secret = _configuration["Paymob:Hmac"] ?? "";
            if (string.IsNullOrEmpty(secret))
                return Result.Failure("HMAC secret not configured");

            var computedSignature = ComputeHmacSha512(requestBody, secret);
            if (!string.Equals(computedSignature, hmacHeader, StringComparison.OrdinalIgnoreCase))
                return Result.Failure("Invalid HMAC signature");

            // Parse webhook payload
            using var doc = JsonDocument.Parse(requestBody);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "";
            var obj = root.TryGetProperty("obj", out var objProp) ? objProp : default;

            if (string.IsNullOrEmpty(eventType) || obj.ValueKind == JsonValueKind.Undefined)
                return Result.Failure("Invalid webhook payload");

            _logger.LogInformation("Paymob webhook received: {EventType}", eventType);

            switch (eventType)
            {
                case "invoice.paid":
                case "transaction.success":
                    await HandleSuccessfulPaymentAsync(obj);
                    break;

                case "transaction.failed":
                    await HandleFailedPaymentAsync(obj);
                    break;

                case "refund.initiated":
                case "refund.success":
                    await HandleRefundAsync(obj);
                    break;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Result.Failure($"Webhook processing failed: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task<string?> GetAuthTokenAsync(string baseUrl, string apiKey)
    {
        var payload = new { api_key = apiKey };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{baseUrl}/api/auth/tokens", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var token) ? token.GetString() : null;
    }

    private async Task<long?> CreatePaymobOrderAsync(string baseUrl, string token, int amountCents)
    {
        var payload = new
        {
            auth_token = token,
            delivery_needed = "false",
            amount_cents = amountCents,
            currency = "EGP",
            items = Array.Empty<object>()
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{baseUrl}/api/ecommerce/orders", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetInt64() : null;
    }

    private async Task<string?> GetPaymentKeyAsync(
        string baseUrl, string token, int amountCents,
        long paymobOrderId, int integrationId, object billingData)
    {
        var payload = new
        {
            auth_token = token,
            amount_cents = amountCents,
            expiration = 3600,
            order_id = paymobOrderId,
            billing_data = billingData,
            currency = "EGP",
            integration_id = integrationId,
            lock_order_when_paid = "false"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{baseUrl}/api/acceptance/payment_keys", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() : null;
    }

    private async Task HandleSuccessfulPaymentAsync(JsonElement obj)
    {
        var paymobOrderId = obj.TryGetProperty("order", out var orderProp)
            ? (orderProp.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : null)
            : obj.TryGetProperty("order_id", out var oid) ? oid.GetInt64().ToString() : null;

        if (paymobOrderId == null)
        {
            // Try merchant_order_id
            paymobOrderId = obj.TryGetProperty("merchant_order_id", out var mo) ? mo.GetInt64().ToString() : null;
        }

        if (paymobOrderId == null)
        {
            _logger.LogWarning("No order ID found in webhook payload");
            return;
        }

        var result = await CapturePaymentAsync(paymobOrderId);
        if (!result.IsSuccess)
            _logger.LogWarning("Failed to capture payment for order {PaymobOrderId}: {Error}",
                paymobOrderId, string.Join(", ", result.Errors ?? []));
    }

    private async Task HandleFailedPaymentAsync(JsonElement obj)
    {
        var paymobOrderId = obj.TryGetProperty("order", out var orderProp)
            ? (orderProp.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : null)
            : obj.TryGetProperty("order_id", out var oid) ? oid.GetInt64().ToString() : null;

        if (paymobOrderId == null) return;

        var orders = _unitOfWork.Repository<Order, Guid>();
        var query = await orders.GetAllAsync();
        var order = await query.FirstOrDefaultAsync(o => o.PaymentIntentId == paymobOrderId);

        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            await orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task HandleRefundAsync(JsonElement obj)
    {
        var paymobOrderId = obj.TryGetProperty("order", out var orderProp)
            ? (orderProp.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : null)
            : obj.TryGetProperty("order_id", out var oid) ? oid.GetInt64().ToString() : null;

        if (paymobOrderId == null) return;

        var orders = _unitOfWork.Repository<Order, Guid>();
        var query = await orders.GetAllAsync();
        var order = await query.FirstOrDefaultAsync(o => o.PaymentIntentId == paymobOrderId);

        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Refunded;
            order.Status = OrderStatus.Refunded;
            await orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static string ComputeHmacSha512(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(bodyBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
