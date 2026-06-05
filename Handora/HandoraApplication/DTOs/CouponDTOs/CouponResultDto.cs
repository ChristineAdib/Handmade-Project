namespace HandoraApplication.DTOs.CouponDTOs
{
    public class CouponResultDto
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
