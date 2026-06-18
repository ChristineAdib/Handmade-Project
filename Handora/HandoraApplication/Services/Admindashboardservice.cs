using HandoraApplication.DTOs.AdminDashboardDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.CouponEntities;
using HandoraDomain.Models.OrderEntity;
using HandoraDomain.Models.PaymentEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{

    public class AdminDashboardService(IUnitOfWork unitOfWork, IUserStatsRepository userStatsRepo)
        : IAdminDashboardService
    {
        private readonly IUnitOfWork _uow = unitOfWork;
        private readonly IUserStatsRepository _userStatsRepo = userStatsRepo;

        // ─── helpers ──────────────────────────────────────────────────────────────

        /// Orders that count as real revenue (paid + not cancelled/refunded)
        private static IQueryable<Order> SaleOrders(IQueryable<Order> q) =>
            q.Where(o => !o.IsDeleted
                      && o.PaymentStatus == PaymentStatus.Paid
                      && o.Status != OrderStatus.Cancelled
                      && o.Status != OrderStatus.Refunded);

        private static DateTime StartOfWeek(DateTime d)
        {
            var day = d.Date;
            int diff = (7 + (day.DayOfWeek - DayOfWeek.Monday)) % 7;
            return DateTime.SpecifyKind(day.AddDays(-diff), DateTimeKind.Utc);
        }

        // ─── 1. Sales Summary ────────────────────────────────────────────────────

        public async Task<Result<SalesSummaryDto>> GetSalesSummaryAsync()
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var sales = SaleOrders(orders);

            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = StartOfWeek(now);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dto = new SalesSummaryDto
            {
                TodaySales = await sales
                    .Where(o => o.OrderDate >= startOfToday)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,

                WeekSales = await sales
                    .Where(o => o.OrderDate >= startOfWeek)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,

                MonthSales = await sales
                    .Where(o => o.OrderDate >= startOfMonth)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
            };

            return Result<SalesSummaryDto>.Success(dto);
        }

        // ─── 2. Counts Summary ───────────────────────────────────────────────────

        public async Task<Result<CountsSummaryDto>> GetCountsSummaryAsync()
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var activeOrders = orders.Where(o => !o.IsDeleted);

            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfWeek = StartOfWeek(now);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dto = new CountsSummaryDto
            {
                // orders
                NewOrdersToday = await activeOrders.CountAsync(o => o.OrderDate >= startOfToday),
                NewOrdersThisWeek = await activeOrders.CountAsync(o => o.OrderDate >= startOfWeek),
                NewOrdersThisMonth = await activeOrders.CountAsync(o => o.OrderDate >= startOfMonth),
                TotalOrders = await activeOrders.CountAsync(),

                // buyers (Buyer role)
                NewUsersToday = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Buyer, startOfToday),
                NewUsersThisWeek = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Buyer, startOfWeek),
                NewUsersThisMonth = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Buyer, startOfMonth),
                TotalUsers = await _userStatsRepo.GetTotalUsersInRoleAsync(AppRoles.Buyer),

                // sellers
                NewSellersToday = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Seller, startOfToday),
                NewSellersThisWeek = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Seller, startOfWeek),
                NewSellersThisMonth = await _userStatsRepo.GetNewUsersInRoleCountAsync(AppRoles.Seller, startOfMonth),
                TotalSellers = await _userStatsRepo.GetTotalUsersInRoleAsync(AppRoles.Seller),
            };

            return Result<CountsSummaryDto>.Success(dto);
        }

        // ─── 3. Revenue Chart ────────────────────────────────────────────────────

        public async Task<Result<RevenueChartDto>> GetRevenueChartAsync(RevenueChartPeriod period)
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var sales = SaleOrders(orders);
            var now = DateTime.UtcNow;
            var points = new List<RevenueChartPointDto>();

            switch (period)
            {
                case RevenueChartPeriod.Daily:
                    {
                        var from = now.Date.AddDays(-29);

                        // pull only needed columns to memory
                        var rows = await sales
                            .Where(o => o.OrderDate >= from)
                            .Select(o => new { o.OrderDate, o.TotalAmount })
                            .ToListAsync();

                        var grouped = rows
                            .GroupBy(o => o.OrderDate.Date)
                            .ToDictionary(g => g.Key,
                                          g => (Rev: g.Sum(x => x.TotalAmount), Cnt: g.Count()));

                        for (var d = from; d <= now.Date; d = d.AddDays(1))
                        {
                            grouped.TryGetValue(d, out var a);
                            points.Add(new RevenueChartPointDto
                            {
                                Date = d,
                                Label = d.ToString("dd MMM"),
                                Revenue = a.Rev,
                                OrdersCount = a.Cnt
                            });
                        }
                        break;
                    }

                case RevenueChartPeriod.Weekly:
                    {
                        var thisWeek = StartOfWeek(now);
                        var from = thisWeek.AddDays(-7 * 11);   // 12 weeks total

                        var rows = await sales
                            .Where(o => o.OrderDate >= from)
                            .Select(o => new { o.OrderDate, o.TotalAmount })
                            .ToListAsync();

                        var grouped = rows
                            .GroupBy(o => StartOfWeek(o.OrderDate))
                            .ToDictionary(g => g.Key,
                                          g => (Rev: g.Sum(x => x.TotalAmount), Cnt: g.Count()));

                        for (var ws = from; ws <= thisWeek; ws = ws.AddDays(7))
                        {
                            grouped.TryGetValue(ws, out var a);
                            points.Add(new RevenueChartPointDto
                            {
                                Date = ws,
                                Label = $"{ws:dd MMM} – {ws.AddDays(6):dd MMM}",
                                Revenue = a.Rev,
                                OrdersCount = a.Cnt
                            });
                        }
                        break;
                    }

                case RevenueChartPeriod.Monthly:
                    {
                        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var from = thisMonth.AddMonths(-11);   // 12 months total

                        var rows = await sales
                            .Where(o => o.OrderDate >= from)
                            .Select(o => new { o.OrderDate, o.TotalAmount })
                            .ToListAsync();

                        var grouped = rows
                            .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1,
                                                      0, 0, 0, DateTimeKind.Utc))
                            .ToDictionary(g => g.Key,
                                          g => (Rev: g.Sum(x => x.TotalAmount), Cnt: g.Count()));

                        for (var ms = from; ms <= thisMonth; ms = ms.AddMonths(1))
                        {
                            grouped.TryGetValue(ms, out var a);
                            points.Add(new RevenueChartPointDto
                            {
                                Date = ms,
                                Label = ms.ToString("MMM yyyy"),
                                Revenue = a.Rev,
                                OrdersCount = a.Cnt
                            });
                        }
                        break;
                    }
            }

            return Result<RevenueChartDto>.Success(new RevenueChartDto { Period = period, Points = points });
        }

        // ─── 4. Top Products ─────────────────────────────────────────────────────

        public async Task<Result<List<TopProductDto>>> GetTopProductsAsync(int count = 5)
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var orderItems = _uow.Repository<OrderItem, Guid>().GetAllAsNoTracking().Result;
            var shops = _uow.Repository<Shop, Guid>().GetAllAsNoTracking().Result;

            // join items → paid orders only
            var validItemsQ =
                from oi in orderItems
                join o in SaleOrders(orders) on oi.OrderId equals o.Id
                select new
                {
                    oi.Product.ProductId,
                    oi.Product.ProductName,
                    oi.Product.PictureUrl,
                    oi.ShopId,
                    oi.Quantity,
                    Revenue = oi.Price * oi.Quantity
                };

            var grouped = await validItemsQ
                .GroupBy(x => new { x.ProductId, x.ProductName, x.PictureUrl, x.ShopId })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.PictureUrl,
                    g.Key.ShopId,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(count)
                .ToListAsync();

            var shopIds = grouped.Select(g => g.ShopId).Distinct().ToList();
            var shopDict = await shops
                .Where(s => shopIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            var result = grouped.Select(g => new TopProductDto
            {
                ProductId = g.ProductId,
                ProductName = g.ProductName,
                PictureUrl = g.PictureUrl,
                ShopName = shopDict.TryGetValue(g.ShopId, out var n) ? n : "—",
                QuantitySold = g.QuantitySold,
                TotalRevenue = g.TotalRevenue
            }).ToList();

            return Result<List<TopProductDto>>.Success(result);
        }

        // ─── 5. Top Sellers ──────────────────────────────────────────────────────

        public async Task<Result<List<TopSellerDto>>> GetTopSellersAsync(int count = 5)
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var orderItems = _uow.Repository<OrderItem, Guid>().GetAllAsNoTracking().Result;
            var shops = _uow.Repository<Shop, Guid>().GetAllAsNoTracking().Result;

            var validItemsQ =
                from oi in orderItems
                join o in SaleOrders(orders) on oi.OrderId equals o.Id
                select new { oi.ShopId, oi.OrderId, oi.Quantity, Revenue = oi.Price * oi.Quantity };

            var grouped = await validItemsQ
                .GroupBy(x => x.ShopId)
                .Select(g => new
                {
                    ShopId = g.Key,
                    OrdersCount = g.Select(x => x.OrderId).Distinct().Count(),
                    ProductsSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(count)
                .ToListAsync();

            var shopIds = grouped.Select(g => g.ShopId).ToList();
            // Owner.Name is on Shop entity directly via OwnerId/Owner navigation
            var shopDict = await shops
                .Where(s => shopIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name, s.Logo, s.Rating, s.OwnerId, OwnerName = s.Owner.Name })
                .ToDictionaryAsync(s => s.Id);

            var result = grouped
                .Where(g => shopDict.ContainsKey(g.ShopId))
                .Select(g =>
                {
                    var info = shopDict[g.ShopId];
                    return new TopSellerDto
                    {
                        ShopId = g.ShopId,
                        SellerId = info.OwnerId,
                        SellerName = info.OwnerName,
                        ShopName = info.Name,
                        Logo = info.Logo,
                        OrdersCount = g.OrdersCount,
                        ProductsSold = g.ProductsSold,
                        TotalRevenue = g.TotalRevenue,
                        Rating = info.Rating
                    };
                })
                .ToList();

            return Result<List<TopSellerDto>>.Success(result);
        }

        // ─── 6. Top Buyers (with coupon data) ────────────────────────────────────

        public async Task<Result<List<TopBuyerDto>>> GetTopBuyersAsync(int count = 5)
        {
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;
            var sales = SaleOrders(orders);

            // User.Name & User.Email come from the navigation property on Order
            var grouped = await sales
                .GroupBy(o => new { o.UserId, o.User.Name, o.User.Email })
                .Select(g => new
                {
                    g.Key.UserId,
                    g.Key.Name,
                    g.Key.Email,
                    OrdersCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    CouponsUsedCount = g.Count(o => o.CouponId != null),
                    TotalDiscountReceived = g.Sum(o => o.DiscountAmount ?? 0m)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ThenByDescending(x => x.OrdersCount)
                .Take(count)
                .ToListAsync();

            var result = grouped.Select(g => new TopBuyerDto
            {
                BuyerId = g.UserId,
                BuyerName = g.Name,
                Email = g.Email ?? string.Empty,
                OrdersCount = g.OrdersCount,
                TotalSpent = g.TotalSpent,
                CouponsUsedCount = g.CouponsUsedCount,
                TotalDiscountReceived = g.TotalDiscountReceived
            }).ToList();

            return Result<List<TopBuyerDto>>.Success(result);
        }

        // ─── 7. Coupon Stats ─────────────────────────────────────────────────────

        public async Task<Result<CouponStatsDto>> GetCouponStatsAsync(int topCount = 5)
        {
            var coupons = _uow.Repository<Coupon, Guid>().GetAllAsNoTracking().Result;
            var orders = _uow.Repository<Order, Guid>().GetAllAsNoTracking().Result;

            var now = DateTime.UtcNow;
            var activeCoupons = coupons.Where(c => !c.IsDeleted && c.IsActive && c.ExpiryDate > now);
            var activeCnt = await activeCoupons.CountAsync();

            var redeemedOrders = SaleOrders(orders).Where(o => o.CouponId != null);
            var totalUsed = await redeemedOrders.CountAsync();
            var totalDiscount = await redeemedOrders.SumAsync(o => (decimal?)(o.DiscountAmount ?? 0m)) ?? 0m;

            // top coupons: group redeemed orders by coupon
            var topRaw = await redeemedOrders
                .GroupBy(o => new
                {
                    o.CouponId,
                    o.Coupon!.Code,
                    o.Coupon!.IsActive,
                    o.Coupon!.ExpiryDate
                })
                .Select(g => new
                {
                    CouponId = g.Key.CouponId!.Value,
                    g.Key.Code,
                    g.Key.IsActive,
                    g.Key.ExpiryDate,
                    UsageCount = g.Count(),
                    TotalDiscountGiven = g.Sum(o => o.DiscountAmount ?? 0m)
                })
                .OrderByDescending(x => x.UsageCount)
                .Take(topCount)
                .ToListAsync();

            var dto = new CouponStatsDto
            {
                ActiveCouponsCount = activeCnt,
                TotalCouponsUsedCount = totalUsed,
                TotalDiscountGiven = totalDiscount,
                TopCoupons = topRaw.Select(c => new TopCouponDto
                {
                    CouponId = c.CouponId,
                    Code = c.Code,
                    UsageCount = c.UsageCount,
                    TotalDiscountGiven = c.TotalDiscountGiven,
                    IsActive = c.IsActive,
                    ExpiryDate = c.ExpiryDate
                }).ToList()
            };

            return Result<CouponStatsDto>.Success(dto);
        }

        // ─── 8. Full Dashboard ───────────────────────────────────────────────────

        public async Task<Result<AdminDashboardDto>> GetDashboardAsync(
            RevenueChartPeriod chartPeriod = RevenueChartPeriod.Daily)
        {
            var sales = await GetSalesSummaryAsync();
            var counts = await GetCountsSummaryAsync();
            var chart = await GetRevenueChartAsync(chartPeriod);
            var products = await GetTopProductsAsync();
            var sellers = await GetTopSellersAsync();
            var buyers = await GetTopBuyersAsync();
            var couponStats = await GetCouponStatsAsync();

            var dto = new AdminDashboardDto
            {
                SalesSummary = sales.Data!,
                Counts = counts.Data!,
                RevenueChart = chart.Data!,
                TopProducts = products.Data!,
                TopSellers = sellers.Data!,
                TopBuyers = buyers.Data!,
                CouponStats = couponStats.Data!
            };

            return Result<AdminDashboardDto>.Success(dto);
        }
    }
}
