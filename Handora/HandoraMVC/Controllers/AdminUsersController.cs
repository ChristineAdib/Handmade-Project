using HandoraApplication.IServices;
using HandoraMVC.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraMVC.Controllers
{
    public class AdminUsersController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IShopService _shopService;

        public AdminUsersController(IAuthService authService, IShopService shopService)
        {
            _authService = authService;
            _shopService = shopService;
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
