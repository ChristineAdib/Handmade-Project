using HandoraDomain.Models.CouponEntities;
using System;
using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.CouponDTOs
{
    public class CreateCouponDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderValue { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUsageCount { get; set; }
    }
}
