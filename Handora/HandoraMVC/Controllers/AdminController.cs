using HandoraApplication.DTOs.AdminDashboardDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Mvc;

namespace HandoraMVC.Controllers
{

    public class AdminController(IAdminDashboardService dashboardService) : Controller
    {
        private readonly IAdminDashboardService _dashboardService = dashboardService;

        // ── GET /Admin/Analytics?period=1 ────────────────────────────────────────
        public async Task<IActionResult> Analytics(RevenueChartPeriod period = RevenueChartPeriod.Daily)
        {
            var result = await _dashboardService.GetDashboardAsync(period);

            if (!result.IsSuccess)
                return View("Error");

            ViewData["Title"] = "Dashboard";
            ViewData["SelectedPeriod"] = period;

            return View(result.Data);
        }

        // ── GET /Admin/ChartData?period=2  (AJAX – returns JSON) ────────────────
        [HttpGet]
        public async Task<IActionResult> ChartData(RevenueChartPeriod period = RevenueChartPeriod.Daily)
        {
            var result = await _dashboardService.GetRevenueChartAsync(period);

            if (!result.IsSuccess)
                return BadRequest();

            // Return only what Chart.js needs
            var payload = new
            {
                labels = result.Data!.Points.Select(p => p.Label),
                revenues = result.Data.Points.Select(p => p.Revenue),
                orders = result.Data.Points.Select(p => p.OrdersCount)
            };

            return Json(payload);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsAsRead([FromServices] INotificationService notificationService)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await notificationService.MarkAllAsReadAsync(userId);
            }
            return Redirect(Request.Headers["Referer"].ToString() ?? "/Admin/Analytics");
        }
    }
}
