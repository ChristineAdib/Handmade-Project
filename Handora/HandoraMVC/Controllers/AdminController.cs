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

        [HttpGet]
        public async Task<IActionResult> ClickNotification(Guid id, [FromServices] INotificationService notificationService, [FromServices] ICustomStudioService customStudioService, CancellationToken ct)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var notifications = await notificationService.GetUserNotificationsAsync(userId, new HandoraApplication.DTOs.Common.PaginationQueryDto { PageNumber = 1, PageSize = 100 }, ct);
            var notif = notifications.Items.FirstOrDefault(n => n.Id == id);
            if (notif == null)
            {
                return NotFound();
            }

            await notificationService.MarkAsReadAsync(id, ct);

            // Redirect based on reference type
            if (notif.ReferenceType == "CustomOrder" && notif.ReferenceId.HasValue)
            {
                return RedirectToAction("Details", "Orders", new { id = notif.ReferenceId.Value });
            }

            if (notif.ReferenceType == "CustomRequest" && notif.ReferenceId.HasValue)
            {
                var reqResult = await customStudioService.GetWorkspaceDetailsAsync(notif.ReferenceId.Value, userId, "Admin", ct);
                if (reqResult.IsSuccess && reqResult.Data?.OrderId != null)
                {
                    return RedirectToAction("Details", "Orders", new { id = reqResult.Data.OrderId });
                }
                return RedirectToAction("Details", "AdminCustomStudio", new { id = notif.ReferenceId.Value });
            }

            if (notif.ReferenceType == "Order" && notif.ReferenceId.HasValue)
            {
                return RedirectToAction("Details", "Orders", new { id = notif.ReferenceId.Value });
            }

            // Fallbacks
            if (notif.Type == HandoraDomain.Models.NotificationEntities.NotificationType.OrderStatusChanged || notif.Type == HandoraDomain.Models.NotificationEntities.NotificationType.OrderStatusChanged)
            {
                if (notif.ReferenceId.HasValue)
                {
                    return RedirectToAction("Details", "Orders", new { id = notif.ReferenceId.Value });
                }
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/Admin/Analytics");
        }
    }
}
