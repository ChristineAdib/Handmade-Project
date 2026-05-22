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
    public static class ProductSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            var products = new List<Product>
        {
            // ── Nour Handmade — Jewelry ───────────────────────────────────────
            new()
            {
                TitleEn         = "Silver Moon Necklace",
                TitleAr         = "قلادة القمر الفضية",
                DescriptionEn   = "Handcrafted sterling silver necklace with a crescent moon pendant. Each piece is unique and made to order.",
                DescriptionAr   = "قلادة فضة استرليني مصنوعة يدويًا بقلادة هلال القمر. كل قطعة فريدة ومصنوعة حسب الطلب.",
                Price           = 450.00m,
                Quantity        = 15,
                Status          = ProductStatus.Active,
                AverageRating   = 4.9m,
                ReviewCount     = 38,
                CategoryId      = Guid.Parse("22222222-0000-0000-0000-000000000001"), // Necklaces
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-6),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/silver-moon-1.jpg", IsMain = true },
                    new() { ImageUrl = "products/silver-moon-2.jpg", IsMain = false },
                }
            },
            new()
            {
                TitleEn         = "Beaded Turquoise Bracelet",
                TitleAr         = "سوار الفيروز المخرز",
                DescriptionEn   = "Handmade bracelet with genuine turquoise beads and silver spacers. Adjustable size.",
                DescriptionAr   = "سوار يدوي من خرز الفيروز الطبيعي مع فواصل فضية. مقاس قابل للتعديل.",
                Price           = 280.00m,
                DiscountPrice   = 230.00m,
                Quantity        = 22,
                Status          = ProductStatus.Active,
                AverageRating   = 4.7m,
                ReviewCount     = 19,
                CategoryId      = Guid.Parse("22222222-0000-0000-0000-000000000002"), // Bracelets
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-5),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/turquoise-bracelet-1.jpg", IsMain = true },
                    new() { ImageUrl = "products/turquoise-bracelet-2.jpg", IsMain = false },
                }
            },
            new()
            {
                TitleEn         = "Pearl Drop Earrings",
                TitleAr         = "أقراط اللؤلؤ المتدلية",
                DescriptionEn   = "Elegant freshwater pearl earrings with gold-plated hooks. Perfect for weddings and special occasions.",
                DescriptionAr   = "أقراط لؤلؤ ماء عذب أنيقة مع خطافات مطلية بالذهب. مثالية للأعراس والمناسبات الخاصة.",
                Price           = 320.00m,
                Quantity        = 10,
                Status          = ProductStatus.Active,
                AverageRating   = 4.8m,
                ReviewCount     = 25,
                CategoryId      = Guid.Parse("22222222-0000-0000-0000-000000000003"), // Earrings
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-4),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/pearl-earrings-1.jpg", IsMain = true },
                }
            },
 
            // ── Layla Crafts — Home Decor ─────────────────────────────────────
            new()
            {
                TitleEn         = "Hand-Painted Ceramic Vase",
                TitleAr         = "مزهرية سيراميك مرسومة يدويًا",
                DescriptionEn   = "Medium-sized ceramic vase hand-painted with geometric patterns inspired by Islamic art. Food-safe glaze.",
                DescriptionAr   = "مزهرية سيراميك متوسطة الحجم مرسومة يدويًا بأنماط هندسية مستوحاة من الفن الإسلامي.",
                Price           = 380.00m,
                Quantity        = 8,
                Status          = ProductStatus.Active,
                AverageRating   = 4.6m,
                ReviewCount     = 14,
                CategoryId      = Guid.Parse("22222222-0000-0000-0000-000000000005"), // Pottery
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-3),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/ceramic-vase-1.jpg", IsMain = true },
                    new() { ImageUrl = "products/ceramic-vase-2.jpg", IsMain = false },
                    new() { ImageUrl = "products/ceramic-vase-3.jpg", IsMain = false },
                }
            },
            new()
            {
                TitleEn         = "Macramé Wall Hanging",
                TitleAr         = "لوحة ماكراميه للحائط",
                DescriptionEn   = "Large boho-style macramé wall hanging made from 100% natural cotton rope. 60cm wide x 90cm long.",
                DescriptionAr   = "لوحة ماكراميه بوهيمية كبيرة مصنوعة من حبل القطن الطبيعي 100%. عرض 60 سم × طول 90 سم.",
                Price           = 550.00m,
                Quantity        = 5,
                Status          = ProductStatus.Active,
                AverageRating   = 4.9m,
                ReviewCount     = 31,
                CategoryId      = Guid.Parse("22222222-0000-0000-0000-000000000004"), // Wall Art
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-4),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/macrame-1.jpg", IsMain = true },
                    new() { ImageUrl = "products/macrame-2.jpg", IsMain = false },
                }
            },
 
            // ── Mariam Art Studio — Paintings ─────────────────────────────────
            new()
            {
                TitleEn         = "Cairo Sunset — Acrylic Painting",
                TitleAr         = "غروب القاهرة — لوحة أكريليك",
                DescriptionEn   = "Original acrylic painting on canvas (50x70cm) depicting a Cairo sunset over the Nile. Signed by the artist. Comes ready to hang.",
                DescriptionAr   = "لوحة أكريليك أصلية على قماش (50×70 سم) تصور غروب القاهرة فوق النيل. موقعة من الفنانة. جاهزة للتعليق.",
                Price           = 1200.00m,
                Quantity        = 1,
                Status          = ProductStatus.Active,
                AverageRating   = 5.0m,
                ReviewCount     = 8,
                CategoryId      = Guid.Parse("11111111-0000-0000-0000-000000000004"), // Art
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-2),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/cairo-sunset-1.jpg", IsMain = true },
                    new() { ImageUrl = "products/cairo-sunset-2.jpg", IsMain = false },
                }
            },
            new()
            {
                TitleEn         = "Botanical Watercolor Print",
                TitleAr         = "لوحة نباتية بالألوان المائية",
                DescriptionEn   = "Delicate watercolor illustration of Egyptian wildflowers. High-quality print on 300gsm paper. A4 size.",
                DescriptionAr   = "رسم مائي رقيق لزهور برية مصرية. طباعة عالية الجودة على ورق 300 جرام. مقاس A4.",
                Price           = 250.00m,
                Quantity        = 20,
                Status          = ProductStatus.Active,
                AverageRating   = 4.7m,
                ReviewCount     = 15,
                CategoryId      = Guid.Parse("11111111-0000-0000-0000-000000000004"), // Art
                ShopId          = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
                CreatedAt       = DateTime.UtcNow.AddMonths(-1),
                Images = new List<ProductImage>
                {
                    new() { ImageUrl = "products/botanical-print-1.jpg", IsMain = true },
                }
            },
        };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
