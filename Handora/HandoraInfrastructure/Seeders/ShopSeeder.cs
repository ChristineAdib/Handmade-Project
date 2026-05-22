using HandoraDomain.Models.ShopEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{
    public static class ShopSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var shops = new List<Shop>
        {
            new()
            {
                Id            = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                Name          = "Nour Handmade",
                DescriptionEn = "Handcrafted jewelry made with love using silver and natural beads.",
                DescriptionAr = "مجوهرات يدوية مصنوعة بحب من الفضة والخرز الطبيعي.",
                Logo          = "shops/nour-logo.jpg",
                OwnerId       = "seller-000-0000-0000-000000000001",
                IsVerified    = true,
                Rating        = 4.8m,
                ReviewCount   = 124,
                TotalSales    = 580,
                CreatedAt     = DateTime.UtcNow.AddMonths(-8),
            },
            new()
            {
                Id            = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                Name          = "Layla Crafts",
                DescriptionEn = "Handmade home decor and painted pottery with earthy natural colors.",
                DescriptionAr = "ديكور منزلي يدوي وفخار مرسوم بألوان طبيعية دافئة.",
                Logo          = "shops/layla-logo.jpg",
                OwnerId       = "seller-000-0000-0000-000000000002",
                IsVerified    = true,
                Rating        = 4.6m,
                ReviewCount   = 89,
                TotalSales    = 340,
                CreatedAt     = DateTime.UtcNow.AddMonths(-5),
            },
            new()
            {
                Id            = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
                Name          = "Mariam Art Studio",
                DescriptionEn = "Original acrylic and watercolor paintings with a contemporary oriental style.",
                DescriptionAr = "لوحات أصلية بالأكريليك والألوان المائية بأسلوب شرقي معاصر.",
                Logo          = "shops/mariam-logo.jpg",
                OwnerId       = "seller-000-0000-0000-000000000003",
                IsVerified    = true,
                Rating        = 4.9m,
                ReviewCount   = 67,
                TotalSales    = 210,
                CreatedAt     = DateTime.UtcNow.AddMonths(-3),
            },
        };

            var existingIds = await context.Shops.Select(s => s.Id).ToListAsync();
            var newShops = shops.Where(s => !existingIds.Contains(s.Id)).ToList();

            if (newShops.Count != 0)
            {
                await context.Shops.AddRangeAsync(newShops);
                await context.SaveChangesAsync();
            }
        }
    }
}
