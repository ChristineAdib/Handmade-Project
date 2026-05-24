namespace HandoraApplication.DTOs.OrderDTOs;

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}
