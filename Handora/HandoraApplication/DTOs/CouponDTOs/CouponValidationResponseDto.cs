using System;

namespace HandoraApplication.DTOs.CouponDTOs
{
    public class CouponValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
