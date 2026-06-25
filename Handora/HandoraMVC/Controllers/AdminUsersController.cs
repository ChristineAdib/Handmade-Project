using HandoraApplication.IServices;
using HandoraMVC.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraMVC.Controllers
{
    public class AdminUsersController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IShopService _shopService;
        private readonly AppDbContext _context;

        public AdminUsersController(IAuthService authService, IShopService shopService, AppDbContext context)
        {
            _authService = authService;
            _shopService = shopService;
            _context = context;
        }

        // GET: /AdminUsers
        public async Task<IActionResult> Index(string? role, string? status)
        {
            bool? isActive = null;
            if (status == "active") isActive = true;
            else if (status == "inactive") isActive = false;

            var usersDto = await _authService.GetUsersFilteredAsync(role, isActive);
            var usersList = new List<UserViewModel>();

            foreach (var userDto in usersDto)
            {
                var userVm = userDto.Adapt<UserViewModel>();
                
                // If filtering by banned
                if (status == "banned" && !userVm.IsBanned)
                {
                    continue;
                }

                // Retrieve shop details for Sellers
                if (userVm.Roles.Contains("Seller"))
                {
                    var shopResult = await _shopService.GetMyShop(userVm.Id);
                    if (shopResult.IsSuccess)
                    {
                        userVm.HasShop = true;
                        userVm.ShopId = shopResult.Data!.Id;
                    }
                }

                usersList.Add(userVm);
            }

            var vm = new UserManagementViewModel
            {
                Users = usersList,
                SelectedRole = role,
                SelectedStatus = status
            };

            return View(vm);
        }

        // GET: /AdminUsers/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var userDto = await _authService.GetUserByIdAsync(id);
            if (userDto == null)
            {
                return NotFound();
            }

            var isSeller = userDto.Roles.Contains("Seller");
            var vm = new UserDetailsViewModel
            {
                Id = userDto.Id,
                Name = userDto.Name,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                ProfileImage = userDto.ProfileImage,
                Bio = userDto.Bio,
                CreatedAt = userDto.CreatedAt,
                Roles = userDto.Roles,
                IsActive = userDto.IsActive,
                IsBanned = userDto.IsBanned,
                IsSeller = isSeller
            };

            if (isSeller)
            {
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.OwnerId == id && !s.IsDeleted);

                if (shop != null)
                {
                    vm.HasShop = true;
                    vm.ShopId = shop.Id;
                    vm.ShopName = shop.Name;
                    vm.ShopRating = shop.Rating;
                    vm.ShopReviewCount = shop.ReviewCount;
                    vm.ShopTotalSales = shop.TotalSales;
                    vm.ShopIsVerified = shop.IsVerified;

                    vm.ShopProductCount = await _context.Products
                        .CountAsync(p => p.ShopId == shop.Id && !p.IsDeleted);

                    vm.ShopOrderCount = await _context.Orders
                        .CountAsync(o => o.Items.Any(i => i.ShopId == shop.Id) && !o.IsDeleted);

                    vm.RecentProducts = await _context.Products
                        .Include(p => p.Images)
                        .Where(p => p.ShopId == shop.Id && !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .Select(p => new UserProductViewModel
                        {
                            Id = p.Id,
                            TitleEn = p.TitleEn,
                            TitleAr = p.TitleAr,
                            Price = p.Price,
                            DiscountPrice = p.DiscountPrice,
                            Status = p.Status.ToString(),
                            ImageUrl = p.Images.FirstOrDefault(i => i.IsMain).ImageUrl ?? p.Images.FirstOrDefault().ImageUrl,
                            CreatedAt = p.CreatedAt
                        })
                        .ToListAsync();
                }
            }
            else
            {
                // Buyer Details
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == id && !a.IsDeleted)
                    .Select(a => new UserAddressViewModel
                    {
                        Id = a.Id,
                        AddressLine = a.AddressLine,
                        City = a.City,
                        Country = a.Country,
                        PostalCode = a.PostalCode
                    })
                    .ToListAsync();
                vm.Addresses = addresses;

                var orders = await _context.Orders
                    .Where(o => o.UserId == id && !o.IsDeleted)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                vm.OrderCount = orders.Count;
                vm.TotalSpent = orders.Sum(o => o.TotalAmount);

                vm.RecentOrders = orders.Take(5).Select(o => new UserOrderDetailViewModel
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Total = o.TotalAmount,
                    Status = o.Status.ToString()
                }).ToList();

                var reviews = await _context.Reviews
                    .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                    .Where(r => r.UserId == id && !r.IsDeleted)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                vm.Reviews = reviews.Select(r => new UserReviewViewModel
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ProductId = r.ProductId,
                    ProductTitle = r.Product.TitleEn,
                    ProductImage = r.Product.Images.FirstOrDefault(i => i.IsMain).ImageUrl ?? r.Product.Images.FirstOrDefault().ImageUrl,
                    CreatedAt = r.CreatedAt
                }).ToList();
            }

            return View(vm);
        }

        // POST: /AdminUsers/ToggleBanStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBanStatus(string userId, string? returnUrl = null)
        {
            var result = await _authService.ToggleUserBanStatusAsync(userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? new[] { "Failed to toggle user ban status." });
            }
            else
            {
                TempData["SuccessMessage"] = "User ban status updated successfully.";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminUsers/ShopDetails/{id}
        public async Task<IActionResult> ShopDetails(Guid id)
        {
            var shopResult = await _shopService.GetShopById(id);
            if (!shopResult.IsSuccess)
            {
                return NotFound();
            }

            var shopDto = shopResult.Data!;
            var statsResult = await _shopService.GetShopStats(id);

            var vm = shopDto.Adapt<ShopDetailsViewModel>();
            vm.ShopId = shopDto.Id; // Ensure mapping

            if (statsResult.IsSuccess)
            {
                vm.ProductCount = statsResult.Data!.ProductCount;
                vm.ActiveProductCount = statsResult.Data.ActiveProductCount;
            }

            return View(vm);
        }

        // POST: /AdminUsers/UpgradeToSeller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeToSeller(string userId)
        {
            var result = await _authService.UpgradeToSellerAsync(userId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? new[] { "Failed to upgrade user." });
            }
            else
            {
                TempData["SuccessMessage"] = "User upgraded to Seller successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminUsers/ToggleShopStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleShopStatus(Guid shopId)
        {
            var result = await _shopService.ToggleShopStatusAsync(shopId);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? new[] { "Failed to toggle shop status." });
            }
            else
            {
                TempData["SuccessMessage"] = "Shop status updated successfully.";
            }

            return RedirectToAction(nameof(ShopDetails), new { id = shopId });
        }
    }
}
