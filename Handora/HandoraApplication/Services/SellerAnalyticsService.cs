using HandoraApplication.DTOs.SellerAnalyticsDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class SellerAnalyticsService(IUnitOfWork unitOfWork) : ISellerAnalyticsService
    {
        private readonly IUnitOfWork _uow = unitOfWork;

        private async Task<Shop?> GetShopByOwnerIdAsync(string ownerId)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();
            return await query
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId && !s.IsDeleted);
        }

        private (DateTime StartDate, DateTime EndDate) ParseDateFilter(AnalyticsFilterDto filter)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            switch (filter.Preset.ToLower())
            {
                case "today":
                    return (today, now);
                case "last7days":
                    return (today.AddDays(-7), now);
                case "last30days":
                    return (today.AddDays(-30), now);
                case "thismonth":
                    return (new DateTime(today.Year, today.Month, 1), now);
                case "last3months":
                case "last90days":
                    return (today.AddDays(-90), now);
                case "thisyear":
                    return (new DateTime(today.Year, 1, 1), now);
                case "custom":
                    return (filter.StartDate ?? today.AddDays(-30), filter.EndDate ?? now);
                default:
                    return (today.AddDays(-30), now);
            }
        }

        public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(string ownerId, AnalyticsFilterDto filter)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<DashboardSummaryDto>.Failure("Shop not found");

            var (startDate, endDate) = ParseDateFilter(filter);
            var duration = endDate - startDate;
            var prevStartDate = startDate - duration;

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            // Current Period
            var currentItems = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate)
                .Select(oi => new { oi.OrderId, oi.Price, oi.Quantity, oi.Order.UserId })
                .ToListAsync();

            // Previous Period
            var prevItems = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= prevStartDate &&
                             oi.Order.OrderDate < startDate)
                .Select(oi => new { oi.OrderId, oi.Price, oi.Quantity })
                .ToListAsync();

            var totalRevenue = currentItems.Sum(oi => oi.Price * oi.Quantity);
            var prevRevenue = prevItems.Sum(oi => oi.Price * oi.Quantity);
            var revenueGrowth = prevRevenue == 0 
                ? (totalRevenue > 0 ? 100m : 0m) 
                : ((totalRevenue - prevRevenue) / prevRevenue) * 100m;

            var completedOrders = currentItems.Select(oi => oi.OrderId).Distinct().Count();
            var prevOrders = prevItems.Select(oi => oi.OrderId).Distinct().Count();
            var ordersGrowth = prevOrders == 0 
                ? (completedOrders > 0 ? 100m : 0m) 
                : ((decimal)(completedOrders - prevOrders) / prevOrders) * 100m;

            var activeProductsCount = await _uow.Repository<Product, Guid>().GetAllAsNoTracking().Result
                .CountAsync(p => p.ShopId == shop.Id && p.Status == ProductStatus.Active && !p.IsDeleted);

            var totalCustomers = currentItems.Select(oi => oi.UserId).Distinct().Count();

            var summary = new DashboardSummaryDto
            {
                SellerName = shop.Owner?.Name ?? "Seller",
                ShopName = shop.Name,
                ShopRating = shop.Rating,
                ShopLogo = shop.Logo,
                TotalRevenue = totalRevenue,
                RevenueGrowthPercent = Math.Round(revenueGrowth, 1),
                CompletedOrders = completedOrders,
                OrdersGrowthPercent = Math.Round(ordersGrowth, 1),
                ActiveProducts = activeProductsCount,
                TotalCustomers = totalCustomers,
                AverageRating = shop.Rating,
                TotalReviews = shop.ReviewCount
            };

            return Result<DashboardSummaryDto>.Success(summary);
        }

        public async Task<Result<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(string ownerId, AnalyticsFilterDto filter)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<RevenueAnalyticsDto>.Failure("Shop not found");

            var (startDate, endDate) = ParseDateFilter(filter);

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var now = DateTime.UtcNow;
            var today = now.Date;

            // Compute fixed metric revenues
            var dailyRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= today)
                .SumAsync(oi => oi.Price * oi.Quantity);

            var weeklyStart = today.AddDays(-7);
            var weeklyRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= weeklyStart)
                .SumAsync(oi => oi.Price * oi.Quantity);

            var monthlyStart = today.AddDays(-30);
            var monthlyRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= monthlyStart)
                .SumAsync(oi => oi.Price * oi.Quantity);

            var yearlyStart = new DateTime(today.Year, 1, 1);
            var yearlyRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= yearlyStart)
                .SumAsync(oi => oi.Price * oi.Quantity);

            // Time-series trend data
            var itemsInRange = orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate);

            var filledTrend = new List<RevenueTrendPointDto>();

            if ((endDate - startDate).TotalDays > 90)
            {
                // Group by month
                var monthlyRaw = await itemsInRange
                    .Select(oi => new { oi.Order.OrderDate.Year, oi.Order.OrderDate.Month, oi.Price, oi.Quantity })
                    .ToListAsync();

                var trendGrouped = monthlyRaw
                    .GroupBy(d => new { d.Year, d.Month })
                    .Select(g => new RevenueTrendPointDto
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Revenue = g.Sum(x => x.Price * x.Quantity)
                    })
                    .ToList();

                var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
                var endMonth = new DateTime(endDate.Year, endDate.Month, 1);
                for (var m = startMonth; m <= endMonth; m = m.AddMonths(1))
                {
                    var existing = trendGrouped.FirstOrDefault(t => t.Date == m);
                    filledTrend.Add(new RevenueTrendPointDto { Date = m, Revenue = existing?.Revenue ?? 0m });
                }
            }
            else
            {
                // Group by day
                var dailyRaw = await itemsInRange
                    .Select(oi => new { Date = oi.Order.OrderDate.Date, oi.Price, oi.Quantity })
                    .ToListAsync();

                var trendGrouped = dailyRaw
                    .GroupBy(d => d.Date)
                    .Select(g => new RevenueTrendPointDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.Price * x.Quantity)
                    })
                    .ToDictionary(t => t.Date, t => t.Revenue);

                for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    filledTrend.Add(new RevenueTrendPointDto
                    {
                        Date = day,
                        Revenue = trendGrouped.TryGetValue(day, out var rev) ? rev : 0m
                    });
                }
            }

            // Category performance calculation
            var productsRepo = _uow.Repository<Product, Guid>();
            var productsQuery = await productsRepo.GetAllAsNoTracking();
            var shopProducts = await productsQuery
                .Include(p => p.Category)
                .Where(p => p.ShopId == shop.Id && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, p => p.Category != null ? (p.Category.NameEn ?? "Uncategorized") : "Uncategorized");

            var rawCategoryData = await itemsInRange
                .Select(oi => new { oi.Product.ProductId, oi.Price, oi.Quantity })
                .ToListAsync();

            var categoryPerformance = rawCategoryData
                .Select(oi => new
                {
                    CategoryName = shopProducts.TryGetValue(oi.ProductId, out var catName) ? catName : "Uncategorized",
                    Revenue = oi.Price * oi.Quantity
                })
                .GroupBy(x => x.CategoryName)
                .Select(g => new CategoryPerformanceDto
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            return Result<RevenueAnalyticsDto>.Success(new RevenueAnalyticsDto
            {
                DailyRevenue = dailyRevenue,
                WeeklyRevenue = weeklyRevenue,
                MonthlyRevenue = monthlyRevenue,
                YearlyRevenue = yearlyRevenue,
                Trend = filledTrend,
                CategoryPerformance = categoryPerformance
            });
        }

        public async Task<Result<OrdersAnalyticsDto>> GetOrdersAnalyticsAsync(string ownerId, AnalyticsFilterDto filter)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<OrdersAnalyticsDto>.Failure("Shop not found");

            var (startDate, endDate) = ParseDateFilter(filter);

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var itemsInRange = orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate);

            var filledTrend = new List<OrderTrendPointDto>();

            if ((endDate - startDate).TotalDays > 90)
            {
                // Monthly Grouping
                var monthlyRaw = await itemsInRange
                    .Select(oi => new { oi.Order.OrderDate.Year, oi.Order.OrderDate.Month, oi.OrderId })
                    .Distinct()
                    .ToListAsync();

                var trendGrouped = monthlyRaw
                    .GroupBy(d => new { d.Year, d.Month })
                    .Select(g => new OrderTrendPointDto
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        OrdersCount = g.Count()
                    })
                    .ToList();

                var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
                var endMonth = new DateTime(endDate.Year, endDate.Month, 1);
                for (var m = startMonth; m <= endMonth; m = m.AddMonths(1))
                {
                    var existing = trendGrouped.FirstOrDefault(t => t.Date == m);
                    filledTrend.Add(new OrderTrendPointDto { Date = m, OrdersCount = existing?.OrdersCount ?? 0 });
                }
            }
            else
            {
                // Daily Grouping
                var dailyRaw = await itemsInRange
                    .Select(oi => new { Date = oi.Order.OrderDate.Date, oi.OrderId })
                    .Distinct()
                    .ToListAsync();

                var trendGrouped = dailyRaw
                    .GroupBy(d => d.Date)
                    .Select(g => new OrderTrendPointDto
                    {
                        Date = g.Key,
                        OrdersCount = g.Count()
                    })
                    .ToDictionary(t => t.Date, t => t.OrdersCount);

                for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    filledTrend.Add(new OrderTrendPointDto
                    {
                        Date = day,
                        OrdersCount = trendGrouped.TryGetValue(day, out var count) ? count : 0
                    });
                }
            }

            // Month-over-Month & Year-over-Year calculations
            var now = DateTime.UtcNow;
            
            // MoM
            var curMonthStart = new DateTime(now.Year, now.Month, 1);
            var prevMonthStart = curMonthStart.AddMonths(-1);
            
            var curMonthRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= curMonthStart)
                .SumAsync(oi => oi.Price * oi.Quantity);
            var prevMonthRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= prevMonthStart && oi.Order.OrderDate < curMonthStart)
                .SumAsync(oi => oi.Price * oi.Quantity);
            var momRevGrowth = prevMonthRevenue == 0 ? (curMonthRevenue > 0 ? 100m : 0m) : ((curMonthRevenue - prevMonthRevenue) / prevMonthRevenue) * 100m;

            var curMonthOrders = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= curMonthStart)
                .Select(oi => oi.OrderId).Distinct().CountAsync();
            var prevMonthOrders = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= prevMonthStart && oi.Order.OrderDate < curMonthStart)
                .Select(oi => oi.OrderId).Distinct().CountAsync();
            var momOrdGrowth = prevMonthOrders == 0 ? (curMonthOrders > 0 ? 100m : 0m) : ((decimal)(curMonthOrders - prevMonthOrders) / prevMonthOrders) * 100m;

            // YoY
            var curYearStart = new DateTime(now.Year, 1, 1);
            var prevYearStart = curYearStart.AddYears(-1);

            var curYearRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= curYearStart)
                .SumAsync(oi => oi.Price * oi.Quantity);
            var prevYearRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= prevYearStart && oi.Order.OrderDate < curYearStart)
                .SumAsync(oi => oi.Price * oi.Quantity);
            var yoyRevGrowth = prevYearRevenue == 0 ? (curYearRevenue > 0 ? 100m : 0m) : ((curYearRevenue - prevYearRevenue) / prevYearRevenue) * 100m;

            var curYearOrders = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= curYearStart)
                .Select(oi => oi.OrderId).Distinct().CountAsync();
            var prevYearOrders = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered && oi.Order.OrderDate >= prevYearStart && oi.Order.OrderDate < curYearStart)
                .Select(oi => oi.OrderId).Distinct().CountAsync();
            var yoyOrdGrowth = prevYearOrders == 0 ? (curYearOrders > 0 ? 100m : 0m) : ((decimal)(curYearOrders - prevYearOrders) / prevYearOrders) * 100m;

            return Result<OrdersAnalyticsDto>.Success(new OrdersAnalyticsDto
            {
                Trend = filledTrend,
                MonthOverMonthRevenueGrowth = Math.Round(momRevGrowth, 1),
                MonthOverMonthOrdersGrowth = Math.Round(momOrdGrowth, 1),
                YearOverYearRevenueGrowth = Math.Round(yoyRevGrowth, 1),
                YearOverYearOrdersGrowth = Math.Round(yoyOrdGrowth, 1)
            });
        }

        public async Task<Result<CustomerAnalyticsDto>> GetCustomerAnalyticsAsync(string ownerId, AnalyticsFilterDto filter)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<CustomerAnalyticsDto>.Failure("Shop not found");

            var (startDate, endDate) = ParseDateFilter(filter);

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var currentItems = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate)
                .ToListAsync();

            var totalCustomers = currentItems.Select(oi => oi.Order.UserId).Distinct().Count();

            // Returning customers definition: customers with >= 2 Delivered orders overall for this shop
            var allTimeShopOrders = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered)
                .Select(oi => new { oi.Order.UserId, oi.OrderId })
                .Distinct()
                .ToListAsync();

            var customerOrderCounts = allTimeShopOrders
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.UserId, x => x.Count);

            var currentPeriodCustomers = currentItems.Select(oi => oi.Order.UserId).Distinct().ToList();
            var returningCustomersCount = currentPeriodCustomers.Count(userId => customerOrderCounts.TryGetValue(userId, out var count) && count >= 2);
            
            var returningCustomersPercent = totalCustomers == 0 ? 0m : ((decimal)returningCustomersCount / totalCustomers) * 100m;

            // New customers count: customers whose first purchase ever from this shop is within the range
            var newCustomersCount = 0;
            foreach (var userId in currentPeriodCustomers)
            {
                var hasPriorPurchase = await orderItemsQuery
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ShopId == shop.Id &&
                                    oi.Order.Status == OrderStatus.Delivered &&
                                    oi.Order.UserId == userId &&
                                    oi.Order.OrderDate < startDate);
                if (!hasPriorPurchase)
                    newCustomersCount++;
            }

            var customerRetentionRate = totalCustomers == 0 ? 0m : ((decimal)returningCustomersCount / totalCustomers) * 100m;

            // Most Valuable Customer
            var topCustomer = await orderItemsQuery
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => new { oi.Order.UserId, oi.Order.User.Name })
                .Select(g => new TopCustomerDto
                {
                    CustomerName = g.Key.Name ?? "Valued Customer",
                    OrdersCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    TotalSpending = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(c => c.TotalSpending)
                .FirstOrDefaultAsync();

            return Result<CustomerAnalyticsDto>.Success(new CustomerAnalyticsDto
            {
                TotalCustomers = totalCustomers,
                ReturningCustomersCount = returningCustomersCount,
                ReturningCustomersPercent = Math.Round(returningCustomersPercent, 1),
                NewCustomersCount = newCustomersCount,
                CustomerRetentionRate = Math.Round(customerRetentionRate, 1),
                TopCustomer = topCustomer
            });
        }

        public async Task<Result<InventoryAnalyticsDto>> GetInventoryAnalyticsAsync(string ownerId)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<InventoryAnalyticsDto>.Failure("Shop not found");

            var productsRepo = _uow.Repository<Product, Guid>();
            var productsQuery = await productsRepo.GetAllAsNoTracking();

            var shopProducts = await productsQuery
                .Include(p => p.Images)
                .Where(p => p.ShopId == shop.Id && !p.IsDeleted)
                .ToListAsync();

            var lowStock = shopProducts
                .Where(p => p.Quantity <= 5 && p.Quantity > 0)
                .Select(p => new InventoryProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.TitleEn,
                    CurrentStock = p.Quantity,
                    Price = p.Price,
                    PictureUrl = p.Images.FirstOrDefault()?.ImageUrl
                })
                .ToList();

            var outOfStock = shopProducts
                .Where(p => p.Quantity == 0)
                .Select(p => new InventoryProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.TitleEn,
                    CurrentStock = p.Quantity,
                    Price = p.Price,
                    PictureUrl = p.Images.FirstOrDefault()?.ImageUrl
                })
                .ToList();

            // Lowest performing products (Bottom 5 units sold)
            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var salesData = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => oi.Product.ProductId)
                .Select(g => new { ProductId = g.Key, UnitsSold = g.Sum(oi => oi.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitsSold);

            var lowestPerforming = shopProducts
                .Select(p =>
                {
                    var sold = salesData.TryGetValue(p.Id, out var units) ? units : 0;
                    return new LowestPerformingProductDto
                    {
                        ProductId = p.Id,
                        ProductName = p.TitleEn,
                        UnitsSold = sold,
                        CurrentStock = p.Quantity,
                        PictureUrl = p.Images.FirstOrDefault()?.ImageUrl,
                        Recommendation = GetRecommendation(sold, p.Quantity)
                    };
                })
                .OrderBy(x => x.UnitsSold)
                .ThenBy(x => x.CurrentStock)
                .Take(5)
                .ToList();

            return Result<InventoryAnalyticsDto>.Success(new InventoryAnalyticsDto
            {
                LowStockProducts = lowStock,
                OutOfStockProducts = outOfStock,
                LowestPerformingProducts = lowestPerforming
            });
        }

        private string GetRecommendation(int unitsSold, int currentStock)
        {
            if (unitsSold == 0 && currentStock > 10)
                return "Consider lowering price, offering a discount coupon, or updating product photos to boost visibility.";
            if (unitsSold == 0)
                return "Promote this product on social media or review pricing and keywords.";
            if (unitsSold < 3 && currentStock > 20)
                return "Excess inventory. Consider bundling this item with a best seller.";
            return "Consider optimizing the product description or running a limited-time sale.";
        }

        public async Task<Result<RatingAnalyticsDto>> GetRatingAnalyticsAsync(string ownerId)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<RatingAnalyticsDto>.Failure("Shop not found");

            var productsRepo = _uow.Repository<Product, Guid>();
            var productsQuery = await productsRepo.GetAllAsNoTracking();

            var highestRated = await productsQuery
                .Include(p => p.Images)
                .Where(p => p.ShopId == shop.Id && !p.IsDeleted)
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.ReviewCount)
                .Take(5)
                .Select(p => new HighestRatedProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.TitleEn,
                    Rating = p.AverageRating,
                    ReviewsCount = p.ReviewCount,
                    PictureUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().ImageUrl : null
                })
                .ToListAsync();

            var shopReviewsRepo = _uow.Repository<ShopReview, Guid>();
            var shopReviewsQuery = await shopReviewsRepo.GetAllAsNoTracking();

            var reviewsDistribution = await shopReviewsQuery
                .Where(r => r.ShopId == shop.Id && r.IsApproved)
                .GroupBy(r => r.Rating)
                .Select(g => new { Stars = g.Key, Count = g.Count() })
                .ToListAsync();

            var ratingDist = Enumerable.Range(1, 5)
                .Select(stars => new RatingDistributionPointDto
                {
                    Stars = stars,
                    ReviewsCount = reviewsDistribution.FirstOrDefault(d => d.Stars == stars)?.Count ?? 0
                })
                .OrderByDescending(d => d.Stars)
                .ToList();

            return Result<RatingAnalyticsDto>.Success(new RatingAnalyticsDto
            {
                HighestRatedProducts = highestRated,
                RatingDistribution = ratingDist
            });
        }

        public async Task<Result<IEnumerable<SmartInsightDto>>> GetSmartInsightsAsync(string ownerId, AnalyticsFilterDto filter)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<IEnumerable<SmartInsightDto>>.Failure("Shop not found");

            var (startDate, endDate) = ParseDateFilter(filter);

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var currentItems = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate)
                .ToListAsync();

            var insights = new List<SmartInsightDto>();

            // Category Contribution Insight
            var productsRepo = _uow.Repository<Product, Guid>();
            var productsQuery = await productsRepo.GetAllAsNoTracking();
            var shopProducts = await productsQuery
                .Include(p => p.Category)
                .Where(p => p.ShopId == shop.Id && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, p => p.Category?.NameEn ?? "Uncategorized");

            var categorySales = currentItems
                .Select(oi => new
                {
                    Category = shopProducts.TryGetValue(oi.Product.ProductId, out var cat) ? cat : "Uncategorized",
                    Revenue = oi.Price * oi.Quantity
                })
                .GroupBy(x => x.Category)
                .Select(g => new { Category = g.Key, Revenue = g.Sum(x => x.Revenue) })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var totalRev = categorySales.Sum(x => x.Revenue);
            if (totalRev > 0 && categorySales.Any())
            {
                var topCat = categorySales.First();
                var pct = (topCat.Revenue / totalRev) * 100m;
                insights.Add(new SmartInsightDto
                {
                    Text = $"Your \"{topCat.Category}\" category generated {pct:F0}% of total revenue.",
                    Type = "success"
                });
            }

            // Revenue Growth Insight
            var duration = endDate - startDate;
            var prevStartDate = startDate - duration;

            var prevRevenue = await orderItemsQuery
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= prevStartDate &&
                             oi.Order.OrderDate < startDate)
                .SumAsync(oi => oi.Price * oi.Quantity);

            if (totalRev > 0)
            {
                if (prevRevenue > 0)
                {
                    var growth = ((totalRev - prevRevenue) / prevRevenue) * 100m;
                    if (growth > 0)
                    {
                        insights.Add(new SmartInsightDto
                        {
                            Text = $"Sales increased by {growth:F0}% compared to the previous period.",
                            Type = "success"
                        });
                    }
                    else if (growth < 0)
                    {
                        insights.Add(new SmartInsightDto
                        {
                            Text = $"Sales decreased by {Math.Abs(growth):F0}% compared to the previous period. Consider launching a discount coupon to stimulate sales.",
                            Type = "warning"
                        });
                    }
                }
                else
                {
                    insights.Add(new SmartInsightDto
                    {
                        Text = "Sales are up! You had 0 sales in the previous period, but generated revenue this period.",
                        Type = "success"
                    });
                }
            }

            // Top Product Insight
            var topProduct = currentItems
                .GroupBy(oi => oi.Product.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefault();

            if (topProduct != null && topProduct.Qty > 0)
            {
                insights.Add(new SmartInsightDto
                {
                    Text = $"\"{topProduct.Name}\" is currently your best seller with {topProduct.Qty} units sold.",
                    Type = "info"
                });
            }

            // Inventory Insights
            var lowStockCount = await productsQuery
                .CountAsync(p => p.ShopId == shop.Id && !p.IsDeleted && p.Quantity <= 5 && p.Quantity > 0);
            if (lowStockCount > 0)
            {
                insights.Add(new SmartInsightDto
                {
                    Text = $"{lowStockCount} products are running low on stock (stock <= 5). Consider restocking soon.",
                    Type = "warning"
                });
            }

            var outOfStockCount = await productsQuery
                .CountAsync(p => p.ShopId == shop.Id && !p.IsDeleted && p.Quantity == 0);
            if (outOfStockCount > 0)
            {
                insights.Add(new SmartInsightDto
                {
                    Text = $"{outOfStockCount} products are completely sold out.",
                    Type = "danger"
                });
            }

            // Retention Rate Insight
            var allTimeShopOrders = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id && oi.Order.Status == OrderStatus.Delivered)
                .Select(oi => new { oi.Order.UserId, oi.OrderId })
                .Distinct()
                .ToListAsync();

            var customerCounts = allTimeShopOrders
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.UserId, x => x.Count);

            var uniqueCust = currentItems.Select(oi => oi.Order.UserId).Distinct().ToList();
            var returningCount = uniqueCust.Count(userId => customerCounts.TryGetValue(userId, out var count) && count >= 2);
            
            if (uniqueCust.Any())
            {
                var rate = ((decimal)returningCount / uniqueCust.Count) * 100m;
                if (rate >= 30m)
                {
                    insights.Add(new SmartInsightDto
                    {
                        Text = $"Excellent customer loyalty! Your returning customer rate is {rate:F0}%.",
                        Type = "success"
                    });
                }
                else
                {
                    insights.Add(new SmartInsightDto
                    {
                        Text = $"Your returning customer rate is {rate:F0}%. Consider offering a loyalty discount to increase repeat purchases.",
                        Type = "info"
                    });
                }
            }

            return Result<IEnumerable<SmartInsightDto>>.Success(insights);
        }

        public async Task<Result<DrillDownDetailsDto>> GetDrillDownDetailsAsync(string ownerId, DateTime date)
        {
            var shop = await GetShopByOwnerIdAsync(ownerId);
            if (shop == null)
                return Result<DrillDownDetailsDto>.Failure("Shop not found");

            var targetDate = date.Date;

            var orderItemsRepo = _uow.Repository<OrderItem, Guid>();
            var orderItemsQuery = await orderItemsRepo.GetAllAsNoTracking();

            var items = await orderItemsQuery
                .Include(oi => oi.Order)
                .Where(oi => oi.ShopId == shop.Id &&
                             oi.Order.Status == OrderStatus.Delivered &&
                             oi.Order.OrderDate >= targetDate &&
                             oi.Order.OrderDate < targetDate.AddDays(1))
                .ToListAsync();

            var revenue = items.Sum(oi => oi.Price * oi.Quantity);
            var ordersCount = items.Select(oi => oi.OrderId).Distinct().Count();
            var productsSold = items
                .Select(oi => oi.Product != null ? oi.Product.ProductName : "Unknown Product")
                .Distinct()
                .ToList();

            var result = new DrillDownDetailsDto
            {
                Date = targetDate,
                Revenue = revenue,
                OrdersCount = ordersCount,
                ProductsSold = productsSold
            };

            return Result<DrillDownDetailsDto>.Success(result);
        }
    }
}
