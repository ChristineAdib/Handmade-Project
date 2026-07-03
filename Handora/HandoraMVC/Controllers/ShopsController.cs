using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.OrderEntity;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HandoraMVC.Controllers;

public class ShopsController : Controller
{
    private readonly IShopService _shopService;
    private readonly IOrderService _orderService;

    public ShopsController(IShopService shopService, IOrderService orderService)
    {
        _shopService = shopService;
        _orderService = orderService;
    }

    private const int PageSize = 8;

    // GET: /Shops
    public async Task<IActionResult> Index(string? search)
    {
        var result = await _shopService.GetAllShops();
        if (!result.IsSuccess)
            return View("Error");

        var shops = result.Data!.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            shops = shops.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var vm = shops.Select(s => new ShopListItemViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Logo = s.Logo,
            OwnerName = s.OwnerName,
            Rating = s.Rating,
            ReviewCount = s.ReviewCount,
            TotalSales = s.TotalSales,
            ProductCount = s.ProductCount,
            IsVerified = s.IsVerified
        }).ToList();

        ViewData["Search"] = search;
        return View(vm);
    }

    // GET: /Shops/Details/id
    public async Task<IActionResult> Details(
        Guid id,
        string tab = "products",
        int productPage = 1,
        int orderPage = 1,
        OrderStatus? orderStatus = null)
    {
        var shopResult = await _shopService.GetShopWithProducts(id);
        if (!shopResult.IsSuccess)
            return NotFound();

        var statsResult = await _shopService.GetShopStats(id);

        // Products pagination (in-memory)
        var allProducts = shopResult.Data!.Products;
        var totalProducts = statsResult.Data?.ProductCount ?? allProducts.Count;
        var pagedProducts = allProducts
            .Skip((productPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Orders
        var orderQuery = new OrderQueryDto
        {
            PageNumber = orderPage,
            PageSize = PageSize,
            Status = orderStatus,
            SortDescending = true
        };
        var ordersResult = await _orderService.GetSellerOrders(id, orderQuery);

        var vm = new ShopFullViewModel
        {
            Id = id,
            Name = shopResult.Data.Name,
            Logo = shopResult.Data.Logo,
            DescriptionEn = shopResult.Data.DescriptionEn,
            DescriptionAr = shopResult.Data.DescriptionAr,
            OwnerName = shopResult.Data.OwnerName,
            Rating = shopResult.Data.Rating,
            ReviewCount = shopResult.Data.ReviewCount,
            TotalSales = statsResult.Data?.TotalSales ?? 0,
            ProductCount = totalProducts,
            ActiveProductCount = statsResult.Data?.ActiveProductCount ?? 0,

            Products = pagedProducts,
            ProductPage = productPage,
            ProductTotalPages = (int)Math.Ceiling((double)totalProducts / PageSize),

            Orders = ordersResult.Data?.Items.ToList() ?? [],
            OrderPage = orderPage,
            OrderTotalPages = ordersResult.Data != null
                ? (int)Math.Ceiling((double)ordersResult.Data.TotalCount / ordersResult.Data.PageSize)
                : 0,
            SelectedOrderStatus = orderStatus,
            OrderStatusOptions = Enum.GetValues<OrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = orderStatus.HasValue && orderStatus.Value == s
                }).ToList(),

            ActiveTab = tab
        };

        return View(vm);
    }
}