using HandoraDomain.Consts;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ShopEntities;

namespace HandoraDomain.Models.PaymentEntities;

public class SellerBalanceTransaction : BaseEntity<Guid>
{
    public string SellerId { get; set; } = string.Empty;

    public Guid ShopId { get; set; }

    public Guid OrderId { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal NetAmount { get; set; }

    // held until release
    public bool IsReleased { get; set; } = false;

    public DateTime HoldUntil { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public BalanceTransactionType Type { get; set; }

    // navigation
    public User Seller { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public Order Order { get; set; } = null!;
}