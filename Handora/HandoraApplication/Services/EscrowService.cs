using HandoraApplication.Helpers;
using HandoraApplication.IServices;

namespace HandoraApplication.Services;

public class EscrowService : IEscrowService
{
    public Task<Result> CheckAndReleaseFundsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Result> RecordDeliveryAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }
}
