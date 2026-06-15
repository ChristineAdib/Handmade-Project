using HandoraApplication.DTOs.SellerAnalyticsDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HandoraApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Seller)]
    public class SellerAnalyticsController(ISellerAnalyticsService analyticsService) : ControllerBase
    {
        private readonly ISellerAnalyticsService _analyticsService = analyticsService;
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] AnalyticsFilterDto filter)
        {
            var result = await _analyticsService.GetDashboardSummaryAsync(CurrentUserId, filter);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] AnalyticsFilterDto filter)
        {
            var result = await _analyticsService.GetRevenueAnalyticsAsync(CurrentUserId, filter);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] AnalyticsFilterDto filter)
        {
            var result = await _analyticsService.GetOrdersAnalyticsAsync(CurrentUserId, filter);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers([FromQuery] AnalyticsFilterDto filter)
        {
            var result = await _analyticsService.GetCustomerAnalyticsAsync(CurrentUserId, filter);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            var result = await _analyticsService.GetInventoryAnalyticsAsync(CurrentUserId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("ratings")]
        public async Task<IActionResult> GetRatings()
        {
            var result = await _analyticsService.GetRatingAnalyticsAsync(CurrentUserId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("insights")]
        public async Task<IActionResult> GetInsights([FromQuery] AnalyticsFilterDto filter)
        {
            var result = await _analyticsService.GetSmartInsightsAsync(CurrentUserId, filter);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }

        [HttpGet("drilldown")]
        public async Task<IActionResult> GetDrillDown([FromQuery] System.DateTime date)
        {
            var result = await _analyticsService.GetDrillDownDetailsAsync(CurrentUserId, date);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
        }
    }
}
