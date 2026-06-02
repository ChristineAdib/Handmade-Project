using HandoraDomain.Models.CouponEntities;
using System;

namespace HandoraApplication.DTOs.CouponDTOs
{
    public class CouponResponseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public bool IsActive { get; set; }
        public string SellerId { get; set; } = string.Empty;
    }
}
