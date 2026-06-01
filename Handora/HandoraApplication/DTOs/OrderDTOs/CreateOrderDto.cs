using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.OrderDTOs;

public class CreateOrderDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Street { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string Country { get; set; } = string.Empty;

    [Required]
    public Guid DeliveryMethodId { get; set; }

    public string? CouponCode { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
