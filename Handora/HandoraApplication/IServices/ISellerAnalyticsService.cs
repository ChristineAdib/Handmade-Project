using HandoraApplication.DTOs.SellerAnalyticsDTOs;
using HandoraApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface ISellerAnalyticsService
    {
        Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(string ownerId, AnalyticsFilterDto filter);
        Task<Result<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(string ownerId, AnalyticsFilterDto filter);
        Task<Result<OrdersAnalyticsDto>> GetOrdersAnalyticsAsync(string ownerId, AnalyticsFilterDto filter);
        Task<Result<CustomerAnalyticsDto>> GetCustomerAnalyticsAsync(string ownerId, AnalyticsFilterDto filter);
        Task<Result<InventoryAnalyticsDto>> GetInventoryAnalyticsAsync(string ownerId);
        Task<Result<RatingAnalyticsDto>> GetRatingAnalyticsAsync(string ownerId);
        Task<Result<IEnumerable<SmartInsightDto>>> GetSmartInsightsAsync(string ownerId, AnalyticsFilterDto filter);
        Task<Result<DrillDownDetailsDto>> GetDrillDownDetailsAsync(string ownerId, DateTime date);
    }
}
