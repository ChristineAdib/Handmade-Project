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
            if (await context.Categories.AnyAsync()) return;

            // ── Parent Categories ─────────────────────────────────────────────────
            var jewelry = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), NameEn = "Jewelry", NameAr = "مجوهرات", ImageUrl = "categories/jewelry.jpg" };
            var homedecor = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), NameEn = "Home Decor", NameAr = "ديكور المنزل", ImageUrl = "categories/home-decor.jpg" };
            var clothing = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), NameEn = "Clothing", NameAr = "ملابس", ImageUrl = "categories/clothing.jpg" };
            var art = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), NameEn = "Art & Paintings", NameAr = "فن ولوحات", ImageUrl = "categories/art.jpg" };
            var accessories = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), NameEn = "Accessories", NameAr = "إكسسوارات", ImageUrl = "categories/accessories.jpg" };
            var candles = new Category { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), NameEn = "Candles & Scents", NameAr = "شموع وعطور", ImageUrl = "categories/candles.jpg" };

            // ── Sub Categories ────────────────────────────────────────────────────
            var necklaces = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), NameEn = "Necklaces", NameAr = "قلادات", ParentId = jewelry.Id };
            var bracelets = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), NameEn = "Bracelets", NameAr = "أساور", ParentId = jewelry.Id };
            var earrings = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), NameEn = "Earrings", NameAr = "أقراط", ParentId = jewelry.Id };
            var wallart = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), NameEn = "Wall Art", NameAr = "فن الجدران", ParentId = homedecor.Id };
            var pottery = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), NameEn = "Pottery", NameAr = "فخار", ParentId = homedecor.Id };
            var scarves = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000006"), NameEn = "Scarves & Wraps", NameAr = "أوشحة وشالات", ParentId = clothing.Id };
            var bags = new Category { Id = Guid.Parse("22222222-0000-0000-0000-000000000007"), NameEn = "Bags", NameAr = "حقائب", ParentId = accessories.Id };

            await context.Categories.AddRangeAsync(
                jewelry, homedecor, clothing, art, accessories, candles,
                necklaces, bracelets, earrings, wallart, pottery, scarves, bags
            );

            await context.SaveChangesAsync();
        }
    }
}
