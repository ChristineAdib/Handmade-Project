using HandoraDomain.Models.ShopEntities;

namespace HandoraApplication.IServices;

public interface ICommissionService
{
    decimal CalculateCommission(decimal amount, decimal rate);

    decimal CalculateSellerNet(decimal amount, decimal commission);
}