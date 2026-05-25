using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;

namespace HandoraDomain.Models.PaymentEntities
{
    public class WithdrawalRequest : BaseEntity<Guid>
    {
        public string SellerId { get; set; }      // User.Id of seller or Shop.OwnerId
        public Guid ShopId { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public string? TransferReference { get; set; } // e.g., bank txn id or Paymob payout ID

        public User Seller { get; set; } = null!;
        public Shop Shop { get; set; } = null!;
    }

    public enum WithdrawalStatus { Pending, Approved, Paid, Cancelled }
}