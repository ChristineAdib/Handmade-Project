using System;
using System.Collections.Generic;

namespace HandoraApplication.DTOs.SellerAnalyticsDTOs
{
    public class AnalyticsFilterDto
    {
        public string Preset { get; set; } = "last30days"; // today, last7days, last30days, last90days, thisyear, custom
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class DashboardSummaryDto
    {
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public decimal ShopRating { get; set; }
        public string? ShopLogo { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowthPercent { get; set; }
        public int CompletedOrders { get; set; }
        public decimal OrdersGrowthPercent { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    public class RevenueTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class RevenueAnalyticsDto
    {
        public decimal DailyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public List<RevenueTrendPointDto> Trend { get; set; } = [];
        public List<CategoryPerformanceDto> CategoryPerformance { get; set; } = [];
    }

    public class OrderTrendPointDto
    {
        public DateTime Date { get; set; }
        public int OrdersCount { get; set; }
    }

    public class OrdersAnalyticsDto
    {
        public List<OrderTrendPointDto> Trend { get; set; } = [];
        public decimal MonthOverMonthRevenueGrowth { get; set; }
        public decimal MonthOverMonthOrdersGrowth { get; set; }
        public decimal YearOverYearRevenueGrowth { get; set; }
        public decimal YearOverYearOrdersGrowth { get; set; }
    }

    public class TopCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalSpending { get; set; }
    }

    public class CustomerAnalyticsDto
    {
        public int TotalCustomers { get; set; }
        public int ReturningCustomersCount { get; set; }
        public decimal ReturningCustomersPercent { get; set; }
        public int NewCustomersCount { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public TopCustomerDto? TopCustomer { get; set; }
    }

    public class InventoryProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal Price { get; set; }
        public string? PictureUrl { get; set; }
    }

    public class LowestPerformingProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public int CurrentStock { get; set; }
        public string? PictureUrl { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    public class InventoryAnalyticsDto
    {
        public List<InventoryProductDto> LowStockProducts { get; set; } = [];
        public List<InventoryProductDto> OutOfStockProducts { get; set; } = [];
        public List<LowestPerformingProductDto> LowestPerformingProducts { get; set; } = [];
    }

    public class HighestRatedProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int ReviewsCount { get; set; }
        public string? PictureUrl { get; set; }
    }

    public class RatingDistributionPointDto
    {
        public int Stars { get; set; }
        public int ReviewsCount { get; set; }
    }

    public class RatingAnalyticsDto
    {
        public List<HighestRatedProductDto> HighestRatedProducts { get; set; } = [];
        public List<RatingDistributionPointDto> RatingDistribution { get; set; } = [];
    }

    public class SmartInsightDto
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, danger
    }

    public class DrillDownDetailsDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
        public List<string> ProductsSold { get; set; } = [];
    }
}
