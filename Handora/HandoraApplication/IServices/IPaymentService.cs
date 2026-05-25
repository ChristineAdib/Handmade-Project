using HandoraApplication.Helpers;
using HandoraDomain.Models.OrderEntity;

namespace HandoraApplication.IServices;

public interface IPaymentService
{
    Task<Result<string>> CreatePaymentIntentAsync(Order order);
    Task<Result> CapturePaymentAsync(string paymentIntentId);
    Task<Result> VerifyWebhookAsync(string requestBody, string hmacHeader);
}