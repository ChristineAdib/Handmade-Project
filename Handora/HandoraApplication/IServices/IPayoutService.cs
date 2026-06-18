using HandoraApplication.Helpers;
using HandoraApplication.DTOs.Payments;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.PaymentEntities;

namespace HandoraApplication.IServices;

public interface IPayoutService
{
    Task<WithdrawalRequest> RequestWithdrawalAsync(User seller, decimal amount);
    Task ProcessPendingWithdrawalsAsync(); // e.g. admin-triggered or automatic
    Task<bool> ExecutePayoutAsync(WithdrawalRequest request);
    Task<Result<SellerWalletDto>> GetSellerWalletAsync(string sellerId);
    Task<Result<BankAccountDto>> GetBankAccountAsync(string sellerId);
    Task<Result<bool>> UpdateBankAccountAsync(string sellerId, BankAccountDto dto);
}
