using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.DTOs.ProductDTOs;
using HandoraDomain.Models.OrderEntity;
using Microsoft.AspNetCore.Mvc.Rendering;
using HandoraApplication.DTOs.FollowDTOs;
using HandoraApplication.DTOs.ReviewDTOs;

namespace HandoraMVC.ViewModels;

public class ShopListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal TotalSales { get; set; }
    public int ProductCount { get; set; }
}

public class ShopFullViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal TotalSales { get; set; }
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }

    // Products tab
    public List<ProductSummaryDto> Products { get; set; } = [];
    public int ProductPage { get; set; }
    public int ProductTotalPages { get; set; }

    // Orders tab
    public List<OrderSummaryDto> Orders { get; set; } = [];
    public int OrderPage { get; set; }
    public int OrderTotalPages { get; set; }
    public OrderStatus? SelectedOrderStatus { get; set; }
    public List<SelectListItem> OrderStatusOptions { get; set; } = [];

    public string ActiveTab { get; set; } = "products";
    // Reviews tab
    public List<ReviewResponseDto> Reviews { get; set; } = [];
    public int ReviewPage { get; set; }
    public int ReviewTotalPages { get; set; }

    // Followers tab
    public List<ShopFollowerDto> Followers { get; set; } = [];
}