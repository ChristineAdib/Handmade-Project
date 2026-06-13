using HandoraDomain.Models.OrderEntity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HandoraMVC.ViewModels;

public class OrderIndexViewModel
{
    public List<OrderSummaryViewModel> Orders { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    // Filter
    public OrderStatus? SelectedStatus { get; set; }
    public List<SelectListItem> StatusOptions { get; set; } = [];
}

public class OrderSummaryViewModel
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDetailsViewModel
{
    public Guid Id { get; set; }
    public string BuyerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    // Shipping
    public string FullName { get; set; } = string.Empty;
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
    public List<OrderItemViewModel> Items { get; set; } = [];
}

public class OrderItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}
