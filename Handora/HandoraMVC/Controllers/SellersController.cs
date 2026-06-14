using HandoraApplication.IServices;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers;

public class SellersController : Controller
{
    private readonly ISellerService _sellerService;
    private readonly IShopService _shopService;

    public SellersController(ISellerService sellerService, IShopService shopService)
    {
        _sellerService = sellerService;
        _shopService = shopService;
    }

    // GET: /Sellers
    public async Task<IActionResult> Index()
    {
        var result = await _sellerService.GetAllSellers();
        if (!result.IsSuccess)
            return View("Error");

        var vm = new SellerIndexViewModel
        {
            Sellers = result.Data!.Select(s => new SellerCardViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Bio = s.Bio,
                ProfileImage = s.ProfileImage,
                ShopId = s.ShopId,
                ShopName = s.ShopName,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount,
                IsVerified = s.IsVerified,
                MemberSince = s.MemberSince
            }).ToList()
        };

        return View(vm);
    }

    // GET: /Sellers/Shop/id
    public async Task<IActionResult> Shop(Guid id)
    {
        var result = await _shopService.GetShopById(id);
        if (!result.IsSuccess)
            return NotFound();

        var vm = new ShopDetailsViewModel
        {
            Id = result.Data!.Id,
            Name = result.Data.Name,
            DescriptionEn = result.Data.DescriptionEn,
            DescriptionAr = result.Data.DescriptionAr,
            Logo = result.Data.Logo,
            Rating = result.Data.Rating,
            ReviewCount = result.Data.ReviewCount,
            TotalSales = result.Data.TotalSales,
            IsVerified = result.Data.IsVerified,
            OwnerName = result.Data.OwnerName,
            ProductCount = result.Data.ProductCount
        };

        return View(vm);
    }

    // POST: /Sellers/Approve/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        await _sellerService.ApproveSeller(id);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Sellers/Suspend/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string id)
    {
        await _sellerService.SuspendSeller(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsuspend(string id)
    {
        await _sellerService.UnsuspendSeller(id);
        return RedirectToAction(nameof(Index));
    }
}