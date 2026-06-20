using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.OrderEntity;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HandoraMVC.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: /Orders
    public async Task<IActionResult> Index(OrderStatus? status, int page = 1)
    {
        var query = new OrderQueryDto
        {
            PageNumber = page,
            PageSize = 10,
            Status = status,
            SortDescending = true
        };

        var result = await _orderService.GetAllOrders(query);
        if (!result.IsSuccess)
            return View("Error");

        var vm = new OrderIndexViewModel
        {
            Orders = result.Data!.Items.Select(o => new OrderSummaryViewModel
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                Total = o.Total,
                ItemCount = o.ItemCount
            }).ToList(),
            TotalCount = result.Data!.TotalCount,
            PageNumber = result.Data!.PageNumber,
            PageSize = result.Data!.PageSize,
            SelectedStatus = status,
            StatusOptions = Enum.GetValues<OrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = status.HasValue && status.Value == s
                }).ToList()
        };

        return View(vm);
    }

    // GET: /Orders/Details/id
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _orderService.GetOrderByIdForAdmin(id);
        if (!result.IsSuccess)
            return NotFound();

        var o = result.Data!;
        var vm = new OrderDetailsViewModel
        {
            Id = o.Id,
            BuyerEmail = o.BuyerEmail,
            OrderDate = o.OrderDate,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            FullName = $"{o.FirstName} {o.LastName}",
            Street = o.Street,
            City = o.City,
            Country = o.Country,
            DeliveryMethodName = o.DeliveryMethodName,
            DeliveryMethodCost = o.DeliveryMethodCost,
            SubTotal = o.SubTotal,
            DiscountAmount = o.DiscountAmount,
            Total = o.Total,
            Notes = o.Notes,
            CouponCode = o.CouponCode,
            Items = o.Items.Select(i => new OrderItemViewModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                PictureUrl = i.PictureUrl,
                Quantity = i.Quantity,
                Price = i.Price,
                Total = i.Total,
                ShopName = i.ShopName,
                SellerName = i.SellerName,
                SellerEmail = i.SellerEmail,
                SellerPhone = i.SellerPhone
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, OrderStatus status)
    {
        var dto = new UpdateOrderStatusDto { Status = status };
        var userId = User.Identity?.Name ?? "Admin";
        var result = await _orderService.UpdateOrderStatus(id, dto, userId, isAdmin: true);
        if (!result.IsSuccess)
        {
            TempData["Error"] = string.Join(", ", result.Errors);
        }
        else
        {
            TempData["Success"] = "Order status updated successfully.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}