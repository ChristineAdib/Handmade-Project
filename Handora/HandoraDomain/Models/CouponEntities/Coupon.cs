using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.OrderEntity;
using System;
using System.Collections.Generic;

namespace HandoraDomain.Models.CouponEntities
{
    public class Coupon : BaseEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // FK to AspNetUsers (Seller) — nullable to allow coupons without a user
        public string? SellerId { get; set; }
        public User? Seller { get; set; }

        // One coupon can be used across many orders
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
