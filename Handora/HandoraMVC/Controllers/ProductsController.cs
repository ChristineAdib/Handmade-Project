using HandoraApplication.DTOs.ProductDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.ProductEntities;
using HandoraMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraMVC.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductsController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        // GET: /Products
        public async Task<IActionResult> Index(Guid? categoryId, ProductStatus? status, string? search, int page = 1)
        {
            var queryDto = new ProductQueryDto
            {
                PageNumber = page,
                PageSize = 10,
                CategoryId = categoryId,
                Status = status,
                Search = search,
                IsAdmin = true,
                SortBy = "newest",
                SortDescending = true
            };

            var productsResult = await _productService.GetProducts(queryDto);
            if (!productsResult.IsSuccess)
                return View("Error");

            var categoriesResult = await _categoryService.GetAllCategories();
            var categoryOptions = new List<SelectListItem>();
            if (categoriesResult.IsSuccess && categoriesResult.Data != null)
            {
                categoryOptions = categoriesResult.Data.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.NameEn,
                    Selected = categoryId.HasValue && categoryId.Value == c.Id
                }).ToList();
            }

            var statusOptions = Enum.GetValues<ProductStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = status.HasValue && status.Value == s
                }).ToList();

            var viewModel = new ProductListViewModel
            {
                Products = productsResult.Data!.Items.Select(p => new ProductItemViewModel
                {
                    Id = p.Id,
                    TitleEn = p.TitleEn,
                    TitleAr = p.TitleAr,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    MainImageUrl = p.MainImageUrl,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    CategoryNameEn = p.CategoryNameEn,
                    ShopName = p.ShopName,
                    Status = p.Status,
                    Quantity = p.Quantity
                }).ToList(),
                Categories = categoryOptions,
                Statuses = statusOptions,
                SelectedCategoryId = categoryId,
                SelectedStatus = status,
                SearchQuery = search,
                PageNumber = productsResult.Data.PageNumber,
                PageSize = productsResult.Data.PageSize,
                TotalCount = productsResult.Data.TotalCount
            };

            return View(viewModel);
        }

        // POST: /Products/Approve/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var updateDto = new UpdateProductDto
            {
                Status = ProductStatus.Active
            };

            var result = await _productService.UpdateProduct(id, updateDto);
            if (!result.IsSuccess)
            {
                TempData["Error"] = string.Join(", ", result.Errors ?? new[] { "Failed to approve product" });
            }
            else
            {
                TempData["Success"] = "Product approved successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Products/Reject/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            var updateDto = new UpdateProductDto
            {
                Status = ProductStatus.Inactive
            };

            var result = await _productService.UpdateProduct(id, updateDto);
            if (!result.IsSuccess)
            {
                TempData["Error"] = string.Join(", ", result.Errors ?? new[] { "Failed to reject product" });
            }
            else
            {
                TempData["Success"] = "Product rejected successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/id
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.GetProduct(id);
            if (!result.IsSuccess)
                return NotFound();

            var p = result.Data!;
            var parsedStatus = Enum.TryParse<ProductStatus>(p.Status, out var statusVal) ? statusVal : ProductStatus.Inactive;

            var viewModel = new ProductItemViewModel
            {
                Id = p.Id,
                TitleEn = p.TitleEn,
                TitleAr = p.TitleAr,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                CategoryNameEn = p.CategoryNameEn,
                ShopName = p.ShopName,
                Status = parsedStatus,
                Quantity = p.Quantity
            };

            return View(viewModel);
        }

        // POST: /Products/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _productService.DeleteProduct(id);
            if (!result.IsSuccess)
            {
                TempData["Error"] = "Failed to delete product.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
