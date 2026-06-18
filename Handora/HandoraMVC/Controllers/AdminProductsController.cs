using HandoraApplication.IServices;
using HandoraMVC.ViewModels;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers
{
    // [Authorize(Roles = "Admin")] // TODO: Uncomment when MVC login is implemented
    public class AdminProductsController : Controller
    {
        private readonly IProductService _productService;

        public AdminProductsController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _productService.GetProduct(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return NotFound(result.Errors);
            }

            var viewModel = result.Data.Adapt<ProductDetailsViewModel>();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _productService.ApproveProductAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? Array.Empty<string>());
                return RedirectToAction(nameof(Details), new { id = id });
            }

            TempData["SuccessMessage"] = "Product approved successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            var result = await _productService.RejectProductAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? Array.Empty<string>());
                return RedirectToAction(nameof(Details), new { id = id });
            }

            TempData["SuccessMessage"] = "Product archived/rejected successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDraft(Guid id)
        {
            var result = await _productService.ApproveDraftAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? Array.Empty<string>());
                return RedirectToAction(nameof(Details), new { id = id });
            }

            TempData["SuccessMessage"] = "Draft changes approved and applied successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDraft(Guid id)
        {
            var result = await _productService.RejectDraftAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors ?? Array.Empty<string>());
                return RedirectToAction(nameof(Details), new { id = id });
            }

            TempData["SuccessMessage"] = "Draft changes rejected and discarded successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
