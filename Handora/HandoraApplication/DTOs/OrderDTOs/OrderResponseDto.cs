namespace HandoraApplication.DTOs.OrderDTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string BuyerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    // Shipping Address
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    // Delivery
    public string DeliveryMethodName { get; set; } = string.Empty;
    public decimal DeliveryMethodCost { get; set; }

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public string? Notes { get; set; }
    public string? CouponCode { get; set; }
    public string? PaymobOrderId { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = [];
}
