using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.CouponDTOs
{
    public class CouponValidationRequestDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string CartId { get; set; } = string.Empty;
    }
}
