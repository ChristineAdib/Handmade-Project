namespace HandoraApplication.DTOs.Payments;

public class CreateWithdrawalDto
{
    public Guid ShopId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
