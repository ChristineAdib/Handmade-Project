using HandoraApplication.IServices;

namespace HandoraApplication.Services;

public class CommissionService : ICommissionService
{
    public decimal CalculateCommission(decimal amount, decimal rate)
    {
        return Math.Round(amount * rate, 2);
    }

    public decimal CalculateSellerNet(decimal amount, decimal commission)
    {
        return Math.Round(amount - commission, 2);
    }
}
