using System;
using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.CouponDTOs
{
    public class ApplyCouponDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string SellerId { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Order total must be greater than 0")]
        public decimal OrderTotal { get; set; }
    }
}
