using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandoraApplication.DTOs.AdminDashboardDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface IAdminDashboardService
{
    /// <summary>
    /// Total sales (paid orders) for today / this week / this month.
    /// </summary>
    Task<Result<SalesSummaryDto>> GetSalesSummaryAsync();

    /// <summary>
    /// New Orders / Users / Sellers counters (today, week, month) + running totals.
    /// </summary>
    Task<Result<CountsSummaryDto>> GetCountsSummaryAsync();

    /// <summary>
    /// Revenue chart data grouped by day (last 30d), week (last 12w) or month (last 12m).
    /// </summary>
    Task<Result<RevenueChartDto>> GetRevenueChartAsync(RevenueChartPeriod period);

    /// <summary>
    /// Top-N best selling products (by quantity sold), default Top 5.
    /// </summary>
    Task<Result<List<TopProductDto>>> GetTopProductsAsync(int count = 5);

    /// <summary>
    /// Top-N most active sellers (by revenue / orders count), default Top 5.
    /// </summary>
    Task<Result<List<TopSellerDto>>> GetTopSellersAsync(int count = 5);

    /// <summary>
    /// Top-N most active buyers (by total spend / orders count), enriched with
    /// coupon usage information, default Top 5.
    /// </summary>
    Task<Result<List<TopBuyerDto>>> GetTopBuyersAsync(int count = 5);

    /// <summary>
    /// Aggregate coupon usage statistics + top coupons by usage.
    /// </summary>
    Task<Result<CouponStatsDto>> GetCouponStatsAsync(int topCount = 5);

    /// <summary>
    /// Builds the full dashboard payload (everything above) in one call.
    /// </summary>
    Task<Result<AdminDashboardDto>> GetDashboardAsync(RevenueChartPeriod chartPeriod = RevenueChartPeriod.Daily);
}