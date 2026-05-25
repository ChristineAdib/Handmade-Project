using HandoraApplication.Helpers;
using HandoraDomain.Models.OrderEntity;

namespace HandoraApplication.IServices;

public interface IEscrowService
{
    Task<Result> RecordDeliveryAsync(Guid orderId);
    Task<Result> CheckAndReleaseFundsAsync();
}