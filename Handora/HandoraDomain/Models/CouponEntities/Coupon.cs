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

        // FK to AspNetUsers (Seller)
        public string SellerId { get; set; } = string.Empty;
        public User Seller { get; set; } = null!;

        // One coupon can be used across many orders
        public ICollection<Order> Orders { get; set; } = [];
    }
}
