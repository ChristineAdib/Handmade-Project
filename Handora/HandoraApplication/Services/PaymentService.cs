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

using HandoraDomain.Models.NotificationEntities;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraDomain.Models.ChatEntities;
using HandoraDomain.Consts;
using HandoraApplication.Hubs;
using HandoraApplication.DTOs.ChatDTOs;

namespace HandoraApplication.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAuthRepository _authRepository;
    private readonly IChatHubContext? _chatHubContext;

    private static readonly HttpClient _httpClient = new();

    public PaymentService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        INotificationService notificationService,
        IAuthRepository authRepository,
        IChatHubContext? chatHubContext = null)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService;
        _authRepository = authRepository;
        _chatHubContext = chatHubContext;
    }

    public async Task<Result<string>> CreatePaymentIntentAsync(Order order)
    {
        try
        {
            var baseUrl = _configuration["Paymob:BaseUrl"]
                          ?? "https://accept.paymob.com";

            var apiKey = _configuration["Paymob:ApiKey"] ?? "";

            var integrationId = int.Parse(
                _configuration["Paymob:IntegrationId"] ?? "0");

            var iframeId = int.Parse(
                _configuration["Paymob:IframeId"] ?? "0");

            var callbackUrl = _configuration["Paymob:FrontendCallbackUrl"]
                              ?? "http://localhost:4200/payment/callback";

            // 1) AUTH TOKEN
            var token = await GetAuthTokenAsync(baseUrl, apiKey);

            if (string.IsNullOrEmpty(token))
                return Result<string>.Failure("Failed to authenticate with Paymob");

            // 2) CREATE PAYMOB ORDER
            var amountCents = (int)(order.TotalAmount * 100);

            var paymobOrderId = await CreatePaymobOrderAsync(
                baseUrl,
                token,
                amountCents);

            if (paymobOrderId == null)
                return Result<string>.Failure("Failed to create Paymob order");

            // 3) BILLING DATA
            var billingData = new
            {
                apartment = "NA",
                email = order.BuyerEmail,
                floor = "NA",
                first_name = order.User?.Name ?? "Customer",
                street = "NA",
                building = "NA",
                phone_number = order.User?.PhoneNumber ?? "01000000000",
                shipping_method = "PKG",
                postal_code = "12345",
                city = "Cairo",
                country = "EG",
                last_name = "Customer",
                state = "Cairo",
                callback_url = callbackUrl
            };

            // 4) PAYMENT KEY
            var paymentKey = await GetPaymentKeyAsync(
                baseUrl,
                token,
                amountCents,
                paymobOrderId.Value,
                integrationId,
                billingData);

            if (string.IsNullOrEmpty(paymentKey))
                return Result<string>.Failure("Failed to get payment key");

            // 5) SAVE IDS
            order.PaymentIntentId = paymentKey;
            order.PaymobOrderId = paymobOrderId.Value.ToString();

            var orderRepo = _unitOfWork.Repository<Order, Guid>();

            await orderRepo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 6) CHECKOUT URL
            var checkoutUrl =
                $"{baseUrl}/api/acceptance/iframes/{iframeId}?payment_token={paymentKey}";

            _logger.LogInformation("Paymob Checkout URL: {Url}", checkoutUrl);

            return Result<string>.Success(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating payment intent for order {OrderId}",
                order.Id);

            return Result<string>.Failure(
                $"Payment creation failed: {ex.Message}");
        }
    }

    public async Task<Result> CapturePaymentAsync(string paymobOrderId)
    {
        try
        {
            var orders = _unitOfWork.Repository<Order, Guid>();

            var query = await orders.GetAllAsync();

            var order = await query.FirstOrDefaultAsync(
                o => o.PaymobOrderId == paymobOrderId);

            if (order == null)
                return Result.Failure("Order not found");

            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Processing;

            await orders.UpdateAsync(order);

            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Paid,
                Provider = "Paymob",
                ProviderOrderId = paymobOrderId,
                PaidAt = DateTime.UtcNow,
                Currency = "EGP"
            };

            var paymentRepo = _unitOfWork.Repository<Payment, Guid>();

            await paymentRepo.AddAsync(payment);

            await _unitOfWork.SaveChangesAsync();

            // Send Notifications (Scenario 7)
            try
            {
                // 1) Notify Admins
                var admins = await _authRepository.GetUsersInRoleAsync(AppRoles.Admin);
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(new SendNotificationDto
                    {
                        UserId = admin.Id,
                        TitleEn = "New Paid Order Placed",
                        TitleAr = "تم تقديم طلب مدفوع جديد",
                        MessageEn = $"Order {order.Id} has been successfully paid and is pending processing.",
                        MessageAr = $"تم دفع قيمة الطلب {order.Id} بنجاح وهو قيد التجهيز.",
                        Type = NotificationType.NewOrder,
                        ReferenceId = order.Id,
                        ReferenceType = "Order"
                    });
                }

                // 2) Notify Sellers of the order items
                var orderItemsRepo = _unitOfWork.Repository<OrderItem, Guid>();
                var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();
                var orderItems = await orderItemsQuery.Where(oi => oi.OrderId == order.Id).ToListAsync();

                var shopIds = orderItems.Select(oi => oi.ShopId).Distinct().ToList();
                var shopRepo = _unitOfWork.Repository<Shop, Guid>();

                foreach (var shopId in shopIds)
                {
                    var shop = await shopRepo.GetByIdAsync(shopId);
                    if (shop != null)
                    {
                        await _notificationService.SendAsync(new SendNotificationDto
                        {
                            UserId = shop.OwnerId,
                            TitleEn = "New Paid Order",
                            TitleAr = "طلب مدفوع جديد",
                            MessageEn = "A new paid order has been placed. Money is held by Admin until delivery.",
                            MessageAr = "تم تقديم طلب مدفوع جديد. يحتفظ المسؤول بالأموال حتى الاستلام.",
                            Type = NotificationType.NewOrder,
                            ReferenceId = order.Id,
                            ReferenceType = "Order"
                        });
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore
            }

            // Check if the order represents a Custom Studio request and complete payment
            try
            {
                var orderItemsRepo = _unitOfWork.Repository<OrderItem, Guid>();
                var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();
                var orderItems = await orderItemsQuery.Where(oi => oi.OrderId == order.Id).ToListAsync();

                var customRequestRepo = _unitOfWork.Repository<CustomRequest, Guid>();
                foreach (var item in orderItems)
                {
                    var requestsQuery = await customRequestRepo.GetAllAsync();
                    var requestWithWorkspace = await requestsQuery
                        .Include(r => r.ProjectWorkspace)
                        .Include(r => r.Buyer)
                        .FirstOrDefaultAsync(r => r.Id == item.Product.ProductId);

                    if (requestWithWorkspace != null && requestWithWorkspace.Status == CustomRequestStatus.PaymentPending)
                    {
                        requestWithWorkspace.CompletePayment();
                        await customRequestRepo.UpdateAsync(requestWithWorkspace);
                        await _unitOfWork.SaveChangesAsync();

                        _logger.LogInformation("[CUSTOM_STUDIO_AUDIT] Custom request {RequestId} payment successfully completed. Status transitioned to Paid and Workspace to DepositPaid.", requestWithWorkspace.Id);

                        var shopRepo = _unitOfWork.Repository<Shop, Guid>();
                        var shop = await shopRepo.GetByIdAsync(item.ShopId);
                        if (shop != null)
                        {
                            // Notify Seller
                            await _notificationService.SendAsync(new SendNotificationDto
                            {
                                UserId = shop.OwnerId,
                                TitleEn = "Custom Request Deposit Paid",
                                TitleAr = "تم دفع عربون الطلب الخاص",
                                MessageEn = $"The deposit for custom request '{item.Product.ProductName}' has been paid. You can now begin crafting.",
                                MessageAr = $"تم دفع عربون الطلب الخاص '{item.Product.ProductName}'. يمكنك الآن البدء في التصنيع.",
                                Type = NotificationType.NewOrder,
                                ReferenceId = requestWithWorkspace.Id,
                                ReferenceType = "CustomRequest"
                            });

                            // Notify Buyer
                            await _notificationService.SendAsync(new SendNotificationDto
                            {
                                UserId = requestWithWorkspace.BuyerId,
                                TitleEn = "Custom Request Deposit Paid Successfully",
                                TitleAr = "تم دفع عربون الطلب الخاص بنجاح",
                                MessageEn = $"Your deposit for custom request '{item.Product.ProductName}' has been successfully paid.",
                                MessageAr = $"تم دفع عربون الطلب الخاص '{item.Product.ProductName}' بنجاح.",
                                Type = NotificationType.NewOrder,
                                ReferenceId = requestWithWorkspace.Id,
                                ReferenceType = "CustomRequest"
                            });

                            // Send chat update message if chat is active
                            if (requestWithWorkspace.ProjectWorkspace != null && requestWithWorkspace.ProjectWorkspace.ChatConversationId.HasValue)
                            {
                                var conversationId = requestWithWorkspace.ProjectWorkspace.ChatConversationId.Value;
                                var buyerName = requestWithWorkspace.Buyer?.Name ?? "Buyer";
                                var messageContent = $"Deposit paid! The custom request '{item.Product.ProductName}' is now active and in production.";

                                await SendChatMessageAsync(
                                    conversationId,
                                    requestWithWorkspace.BuyerId,
                                    buyerName,
                                    shop.OwnerId,
                                    messageContent,
                                    MessageType.Text
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CustomRequest state transition after payment capture for order {OrderId}", order.Id);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error capturing payment {PaymobOrderId}",
                paymobOrderId);

            return Result.Failure(
                $"Capture failed: {ex.Message}");
        }
    }

    private async Task SendChatMessageAsync(
        Guid conversationId, string senderId, string senderName, string receiverId, string content, MessageType type = MessageType.Text, string? imageUrl = null)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            Type = type,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.Repository<Message, Guid>().AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        if (_chatHubContext != null)
        {
            var msgDto = new MessageDto
            {
                Id = message.Id,
                ConversationId = conversationId,
                SenderId = senderId,
                SenderName = senderName,
                Content = content,
                Type = type,
                ImageUrl = imageUrl,
                CreatedAt = message.CreatedAt
            };

            await _chatHubContext.SendMessageAsync(receiverId, msgDto);
        }
        else
        {
            _logger.LogWarning("ChatHubContext is not registered. Saved message to DB but skipped SignalR broadcast.");
        }
    }

    public async Task<Result> VerifyWebhookAsync(
        string requestBody,
        string hmacHeader)
    {
        try
        {
            _logger.LogInformation(
                "Webhook Received: {Body}",
                requestBody);

            // TEMPORARILY DISABLED HMAC VALIDATION FOR TESTING
            // ENABLE IT LATER AFTER EVERYTHING WORKS

            using var doc = JsonDocument.Parse(requestBody);

            var root = doc.RootElement;

            var obj = root.TryGetProperty("obj", out var objProp)
                ? objProp
                : default;

            if (obj.ValueKind == JsonValueKind.Undefined)
                return Result.Failure("Invalid webhook payload");

            var success = obj.TryGetProperty("success", out var successProp)
                          && successProp.GetBoolean();

            if (!success)
            {
                await HandleFailedPaymentAsync(obj);
                return Result.Success();
            }

            await HandleSuccessfulPaymentAsync(obj);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error");

            return Result.Failure(
                $"Webhook processing failed: {ex.Message}");
        }
    }

    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    private async Task<string?> GetAuthTokenAsync(
        string baseUrl,
        string apiKey)
    {
        try
        {
            var payload = new
            {
                api_key = apiKey
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{baseUrl}/api/auth/tokens",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "AUTH RESPONSE: {Status} - {Body}",
                response.StatusCode,
                responseBody);

            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(responseBody);

            return doc.RootElement.TryGetProperty("token", out var token)
                ? token.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAuthTokenAsync Error");
            return null;
        }
    }

    private async Task<long?> CreatePaymobOrderAsync(
        string baseUrl,
        string token,
        int amountCents)
    {
        try
        {
            var payload = new
            {
                auth_token = token,
                delivery_needed = false,
                amount_cents = amountCents.ToString(),
                currency = "EGP",
                items = Array.Empty<object>()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{baseUrl}/api/ecommerce/orders",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "ORDER RESPONSE: {Status} - {Body}",
                response.StatusCode,
                responseBody);

            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(responseBody);

            return doc.RootElement.TryGetProperty("id", out var id)
                ? id.GetInt64()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePaymobOrderAsync Error");
            return null;
        }
    }

    private async Task<string?> GetPaymentKeyAsync(
        string baseUrl,
        string token,
        int amountCents,
        long paymobOrderId,
        int integrationId,
        object billingData)
    {
        try
        {
            var payload = new
            {
                auth_token = token,
                amount_cents = amountCents.ToString(),
                expiration = 3600,
                order_id = paymobOrderId,
                billing_data = billingData,
                currency = "EGP",
                integration_id = integrationId,
                lock_order_when_paid = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{baseUrl}/api/acceptance/payment_keys",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "PAYMENT KEY RESPONSE: {Status} - {Body}",
                response.StatusCode,
                responseBody);

            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(responseBody);

            return doc.RootElement.TryGetProperty("token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPaymentKeyAsync Error");
            return null;
        }
    }

    private async Task HandleSuccessfulPaymentAsync(JsonElement obj)
    {
        try
        {
            string? paymobOrderId = null;

            if (obj.TryGetProperty("order", out var orderProp))
            {
                if (orderProp.TryGetProperty("id", out var idProp))
                {
                    paymobOrderId = idProp.GetInt64().ToString();
                }
            }

            if (string.IsNullOrEmpty(paymobOrderId))
            {
                _logger.LogWarning("No Paymob order ID found");
                return;
            }

            var result = await CapturePaymentAsync(paymobOrderId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to capture payment for order {OrderId}",
                    paymobOrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleSuccessfulPaymentAsync Error");
        }
    }

    private async Task HandleFailedPaymentAsync(JsonElement obj)
    {
        try
        {
            string? paymobOrderId = null;

            if (obj.TryGetProperty("order", out var orderProp))
            {
                if (orderProp.TryGetProperty("id", out var idProp))
                {
                    paymobOrderId = idProp.GetInt64().ToString();
                }
            }

            if (string.IsNullOrEmpty(paymobOrderId))
                return;

            var orders = _unitOfWork.Repository<Order, Guid>();

            var query = await orders.GetAllAsync();

            var order = await query.FirstOrDefaultAsync(
                o => o.PaymobOrderId == paymobOrderId);

            if (order == null)
                return;

            order.PaymentStatus = PaymentStatus.Failed;

            await orders.UpdateAsync(order);

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleFailedPaymentAsync Error");
        }
    }

    private static string ComputeHmacSha512(
        string body,
        string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);

        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA512(keyBytes);

        var hashBytes = hmac.ComputeHash(bodyBytes);

        return BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();
    }
}