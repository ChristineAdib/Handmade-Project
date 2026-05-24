using HandoraDomain.Models.OrderEntity;

namespace HandoraApplication.DTOs.OrderDTOs;

public class OrderQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public OrderStatus? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
