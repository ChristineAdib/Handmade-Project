using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ShopEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.CouponEntities
{
    public class Coupon:BaseEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DiscountType DiscountType { get; set; }
        public DateTime ExpiresAt { get; set; }
        public decimal? MinOrderAmount { get; set; }    // [IMPROVEMENT] minimum cart value to apply coupon
        public int? MaxUsageCount { get; set; }         // [IMPROVEMENT] null = unlimited
        public int UsageCount { get; set; } = 0;        // [IMPROVEMENT] track how many times it was used
        public bool IsActive { get; set; } = true;      // [IMPROVEMENT] admin can disable without deleting

        // FK
        public Guid ShopId { get; set; }
        public Shop Shop { get; set; } = null!;

        // One coupon can be used across many orders
        public ICollection<Order> Orders { get; set; } = [];
    }
}
