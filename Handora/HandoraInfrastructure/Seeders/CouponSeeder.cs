using HandoraDomain.Models.CouponEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Id            = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                Code          = "WELCOME20",
                Value         = 20,
                DiscountType  = DiscountType.Percentage,
                MinOrderAmount= 200,
                MaxUsageCount = 100,
                UsageCount    = 12,
                IsActive      = true,
                ExpiresAt     = DateTime.UtcNow.AddMonths(3),
                ShopId        = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            },
            new()
            {
                Id            = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
                Code          = "LAYLA50",
                Value         = 50,
                DiscountType  = DiscountType.FixedAmount,
                MinOrderAmount= 300,
                MaxUsageCount = 50,
                UsageCount    = 5,
                IsActive      = true,
                ExpiresAt     = DateTime.UtcNow.AddMonths(2),
                ShopId        = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
            },
            new()
            {
                Id            = Guid.Parse("cccccccc-0000-0000-0000-000000000003"),
                Code          = "ART15",
                Value         = 15,
                DiscountType  = DiscountType.Percentage,
                MinOrderAmount= null,   // no minimum
                MaxUsageCount = null,   // unlimited
                UsageCount    = 3,
                IsActive      = true,
                ExpiresAt     = DateTime.UtcNow.AddMonths(6),
                ShopId        = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
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
