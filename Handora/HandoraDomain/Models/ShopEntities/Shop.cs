using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ProductEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ShopEntities
{
    public class Shop : BaseEntity<Guid>
    {

        public decimal AvailableBalance { get; set; } = 0m; // funds available for withdrawal
        public decimal CommissionRate { get; set; } = 0.10m; // e.g., 10% commission (configurable per shop)
        public decimal PendingBalance { get; set; } = 0m;

        public string Name { get; set; } = string.Empty;

        // [LOCALIZATION] bilingual description
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public string? Logo { get; set; }
        public decimal Rating { get; set; } = 0;       // avg rating — updated via trigger/service
        public int ReviewCount { get; set; } = 0;       // [IMPROVEMENT] needed to display "based on N reviews"
        public decimal TotalSales { get; set; } = 0;
        public bool IsVerified { get; set; } = false;   // [IMPROVEMENT] admin verifies shops before going live

        // FK — string because IdentityUser.Id is string
        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; } = null!;

        // Navigation Properties
        public ICollection<Product> Products { get; set; } = [];
        public ICollection<ShopPolicy> Policies { get; set; } = [];
        public ICollection<Coupon> Coupons { get; set; } = [];






        public ICollection<SellerBalanceTransaction> BalanceTransactions { get; set; } = [];
        public ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = [];
    }
}
