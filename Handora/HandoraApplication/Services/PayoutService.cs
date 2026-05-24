using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.PaymentEntities;

namespace HandoraApplication.Services;

public class PayoutService : IPayoutService
{
    public Task<bool> ExecutePayoutAsync(WithdrawalRequest request)
    {
        throw new NotImplementedException();
    }

    public Task ProcessPendingWithdrawalsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<WithdrawalRequest> RequestWithdrawalAsync(User seller, decimal amount)
    {
        throw new NotImplementedException();
    }
}
