using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Models.OrderEntity;

namespace HandoraApplication.Services;

public class PaymentService : IPaymentService
{
    public Task<Result> CapturePaymentAsync(string paymentIntentId)
    {
        throw new NotImplementedException();
    }

    public Task<Result<string>> CreatePaymentIntentAsync(Order order)
    {
        throw new NotImplementedException();
    }

    public Task<Result> VerifyWebhookAsync(string requestBody, string hmacHeader)
    {
        throw new NotImplementedException();
    }
}
