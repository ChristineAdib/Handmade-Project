using HandoraDomain.Models.ProductEntities;
using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{
    public static class CategorySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var categories = new List<Category>
            {
                // ── Parent Categories ─────────────────────────────────────────────
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), NameEn = "Jewelry", NameAr = "مجوهرات", ImageUrl = "categories/jewelry.jpg" },
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), NameEn = "Home Decor", NameAr = "ديكور المنزل", ImageUrl = "categories/home-decor.jpg" },
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), NameEn = "Clothing", NameAr = "ملابس", ImageUrl = "categories/clothing.jpg" },
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), NameEn = "Art & Paintings", NameAr = "فن ولوحات", ImageUrl = "categories/art.jpg" },
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), NameEn = "Accessories", NameAr = "إكسسوارات", ImageUrl = "categories/accessories.jpg" },
                new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), NameEn = "Candles & Scents", NameAr = "شموع وعطور", ImageUrl = "categories/candles.jpg" },

                // ── Sub Categories ────────────────────────────────────────────────
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), NameEn = "Necklaces", NameAr = "قلادات", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000001") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), NameEn = "Bracelets", NameAr = "أساور", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000001") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), NameEn = "Earrings", NameAr = "أقراط", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000001") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), NameEn = "Wall Art", NameAr = "فن الجدران", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000002") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), NameEn = "Pottery", NameAr = "فخار", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000002") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000006"), NameEn = "Scarves & Wraps", NameAr = "أوشحة وشالات", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000003") },
                new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000007"), NameEn = "Bags", NameAr = "حقائب", ParentId = Guid.Parse("11111111-0000-0000-0000-000000000005") },
            };

            var existingIds = await context.Categories.Select(c => c.Id).ToListAsync();
            var newCategories = categories.Where(c => !existingIds.Contains(c.Id)).ToList();

            if (newCategories.Count != 0)
            {
                await context.Categories.AddRangeAsync(newCategories);
                await context.SaveChangesAsync();
            }
        }
    }
}
