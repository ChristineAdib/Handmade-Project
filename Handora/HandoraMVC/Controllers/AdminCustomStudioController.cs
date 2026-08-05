using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HandoraApplication.IServices;
using HandoraDomain.Models.CustomStudioEntities;
using HandoraDomain.Consts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCustomStudioController : Controller
    {
        private readonly IAdminCustomStudioService _adminCustomStudioService;

        public AdminCustomStudioController(IAdminCustomStudioService adminCustomStudioService)
        {
            _adminCustomStudioService = adminCustomStudioService ?? throw new ArgumentNullException(nameof(adminCustomStudioService));
        }

        // GET: /AdminCustomStudio/Dashboard
        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            var result = await _adminCustomStudioService.GetDashboardMetricsAsync(ct);
            if (!result.IsSuccess)
            {
                return View("Error");
            }
            return View(result.Data);
        }

        // GET: /AdminCustomStudio/Requests
        public async Task<IActionResult> Requests(
            string? search, string? buyerId, string? sellerId, int? status, 
            int? offerStatus, int? paymentStatus, string? productType, 
            DateTime? startDate, DateTime? endDate, string sortBy = "newest", 
            int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _adminCustomStudioService.GetRequestsAsync(
                search, buyerId, sellerId, status, offerStatus, paymentStatus, 
                productType, startDate, endDate, sortBy, pageNumber, pageSize, ct);

            if (!result.IsSuccess)
            {
                return View("Error");
            }

            ViewData["Search"] = search;
            ViewData["Status"] = status;
            ViewData["OfferStatus"] = offerStatus;
            ViewData["PaymentStatus"] = paymentStatus;
            ViewData["SortBy"] = sortBy;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            return View(result.Data);
        }

        // GET: /AdminCustomStudio/Details/{id}
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var result = await _adminCustomStudioService.GetRequestDetailsAsync(id, ct);
            if (!result.IsSuccess)
            {
                return NotFound();
            }
            return View(result.Data);
        }

        // POST: /AdminCustomStudio/CancelRequest/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(Guid id, string reason, CancellationToken ct)
        {
            var result = await _adminCustomStudioService.CancelRequestAsync(id, reason, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = "Failed to cancel request: " + string.Join(", ", result.Errors ?? Enumerable.Empty<string>());
            }
            else
            {
                TempData["Success"] = "Custom request cancelled successfully.";
            }
            return RedirectToAction("Details", new { id });
        }

        // POST: /AdminCustomStudio/ArchiveRequest/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveRequest(Guid id, CancellationToken ct)
        {
            var result = await _adminCustomStudioService.ArchiveRequestAsync(id, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = "Failed to archive request.";
            }
            else
            {
                TempData["Success"] = "Custom request archived successfully.";
            }
            return RedirectToAction("Requests");
        }

        // GET: /AdminCustomStudio/AiGenerations
        public async Task<IActionResult> AiGenerations(
            string? provider, int? status, string? search, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _adminCustomStudioService.GetAiGenerationsAsync(provider, status, search, pageNumber, pageSize, ct);
            if (!result.IsSuccess)
            {
                return View("Error");
            }
            ViewData["Provider"] = provider;
            ViewData["Search"] = search;
            return View(result.Data);
        }

        // GET: /AdminCustomStudio/Artisans
        public async Task<IActionResult> Artisans(CancellationToken ct)
        {
            var result = await _adminCustomStudioService.GetArtisanAnalyticsAsync(ct);
            if (!result.IsSuccess)
            {
                return View("Error");
            }
            return View(result.Data);
        }

        // GET: /AdminCustomStudio/Offers
        public async Task<IActionResult> Offers(
            int? status, string? search, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _adminCustomStudioService.GetOffersAsync(status, search, pageNumber, pageSize, ct);
            if (!result.IsSuccess)
            {
                return View("Error");
            }
            var metricsResult = await _adminCustomStudioService.GetOfferMetricsAsync(ct);
            ViewData["Metrics"] = metricsResult.Data;
            ViewData["Status"] = status;
            ViewData["Search"] = search;
            return View(result.Data);
        }


        // GET: /AdminCustomStudio/Settings
        public async Task<IActionResult> Settings(CancellationToken ct)
        {
            var result = await _adminCustomStudioService.GetSettingsAsync(ct);
            if (!result.IsSuccess)
            {
                return View("Error");
            }
            return View(result.Data);
        }

        // POST: /AdminCustomStudio/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(CustomStudioSetting settings, CancellationToken ct)
        {
            var result = await _adminCustomStudioService.UpdateSettingsAsync(settings, ct);
            if (!result.IsSuccess)
            {
                TempData["Error"] = "Failed to update Custom Studio settings.";
                return View(settings);
            }
            TempData["Success"] = "Settings updated successfully.";
            return RedirectToAction("Settings");
        }


        // GET: /AdminCustomStudio/ExportRequests
        public async Task<IActionResult> ExportRequests(CancellationToken ct)
        {
            var result = await _adminCustomStudioService.ExportRequestsToCsvAsync(ct);
            if (!result.IsSuccess) return BadRequest();
            var bytes = Encoding.UTF8.GetBytes(result.Data ?? string.Empty);
            return File(bytes, "text/csv", $"CustomRequests_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // GET: /AdminCustomStudio/ExportOffers
        public async Task<IActionResult> ExportOffers(CancellationToken ct)
        {
            var result = await _adminCustomStudioService.ExportOffersToCsvAsync(ct);
            if (!result.IsSuccess) return BadRequest();
            var bytes = Encoding.UTF8.GetBytes(result.Data ?? string.Empty);
            return File(bytes, "text/csv", $"CustomOffers_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

    }
}
