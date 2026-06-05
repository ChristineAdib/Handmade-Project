using HandoraDomain.Models.CouponEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{
    public static class CouponSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var coupons = new List<Coupon>
            {
                new()
                {
                    Id                 = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                    Code               = "WELCOME20",
                    DiscountValue      = 20,
                    DiscountType       = DiscountType.Percentage,
                    MinOrderValue      = 200,
                    MaxUsageCount      = 100,
                    CurrentUsageCount  = 12,
                    IsActive           = true,
                    ExpiryDate         = DateTime.UtcNow.AddMonths(3),
                    SellerId           = "seller-000-0000-0000-000000000001",
                },
                new()
                {
                    Id                 = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
                    Code               = "LAYLA50",
                    DiscountValue      = 50,
                    DiscountType       = DiscountType.FixedAmount,
                    MinOrderValue      = 300,
                    MaxUsageCount      = 50,
                    CurrentUsageCount  = 5,
                    IsActive           = true,
                    ExpiryDate         = DateTime.UtcNow.AddMonths(2),
                    SellerId           = "seller-000-0000-0000-000000000002",
                },
                new()
                {
                    Id                 = Guid.Parse("cccccccc-0000-0000-0000-000000000003"),
                    Code               = "ART15",
                    DiscountValue      = 15,
                    DiscountType       = DiscountType.Percentage,
                    MinOrderValue      = null,   // no minimum
                    MaxUsageCount      = null,   // unlimited
                    CurrentUsageCount  = 3,
                    IsActive           = true,
                    ExpiryDate         = DateTime.UtcNow.AddMonths(6),
                    SellerId           = "seller-000-0000-0000-000000000003",
                },
            };

            var existingIds = await context.Coupons.Select(c => c.Id).ToListAsync();
            var newCoupons = coupons.Where(c => !existingIds.Contains(c.Id)).ToList();

            if (newCoupons.Count != 0)
            {
                await context.Coupons.AddRangeAsync(newCoupons);
                await context.SaveChangesAsync();
            }
        }
    }
}
