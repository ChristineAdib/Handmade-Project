using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraInfrastructure.Seeders
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(
       UserManager<User> userManager,
       RoleManager<IdentityRole> roleManager)
        {
            // ── 1. Seed Roles first ───────────────────────────────────────────────
            await SeedRolesAsync(roleManager);

            // ── 2. Seed Admin ─────────────────────────────────────────────────────
            await SeedAdminAsync(userManager);

            // ── 3. Seed Sellers ───────────────────────────────────────────────────
            await SeedSellersAsync(userManager);

            // ── 4. Seed Buyers ────────────────────────────────────────────────────
            await SeedBuyersAsync(userManager);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roles = new[] { AppRoles.Admin, AppRoles.Seller, AppRoles.Buyer };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}

        // ─────────────────────────────────────────────────────────────────────────
        private static async Task SeedAdminAsync(UserManager<User> userManager)
        {
            if (await userManager.FindByEmailAsync("admin@handaura.com") is not null) return;

            var admin = new User
            {
                Id = "admin-0000-0000-0000-000000000001",
                Name = "HandAura Admin",
                Email = "admin@handaura.com",
                UserName = "admin@handaura.com",
                PhoneNumber = "01000000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
            };

            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private static async Task SeedSellersAsync(UserManager<User> userManager)
        {
            var sellers = new[]
            {
            new
            {
                Id    = "seller-000-0000-0000-000000000001",
                Name  = "Nour Handmade",
                Email = "nour@handaura.com",
                Phone = "01011111111",
                Bio   = "أصنع مجوهرات يدوية بالخرز والفضة منذ 5 سنوات. كل قطعة بتتعمل بحب وعناية.",
            },
            new
            {
                Id    = "seller-000-0000-0000-000000000002",
                Name  = "Layla Crafts",
                Email = "layla@handaura.com",
                Phone = "01022222222",
                Bio   = "متخصصة في ديكور المنزل اليدوي والفخار المرسوم بالألوان الطبيعية.",
            },
            new
            {
                Id    = "seller-000-0000-0000-000000000003",
                Name  = "Mariam Art Studio",
                Email = "mariam@handaura.com",
                Phone = "01033333333",
                Bio   = "فنانة تشكيلية — لوحات أكريليك وألوان مائية بأسلوب شرقي معاصر.",
            },
        };

            foreach (var s in sellers)
            {
                if (await userManager.FindByEmailAsync(s.Email) is not null) continue;

                var user = new User
                {
                    Id = s.Id,
                    Name = s.Name,
                    Email = s.Email,
                    UserName = s.Email,
                    PhoneNumber = s.Phone,
                    Bio = s.Bio,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                };

                await userManager.CreateAsync(user, "Seller@123");
                await userManager.AddToRoleAsync(user, AppRoles.Seller);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private static async Task SeedBuyersAsync(UserManager<User> userManager)
        {
            var buyers = new[]
            {
            new { Id = "buyer-0000-0000-0000-000000000001", Name = "Sara Ahmed",   Email = "sara@gmail.com",   Phone = "01099991111" },
            new { Id = "buyer-0000-0000-0000-000000000002", Name = "Mona Khaled",  Email = "mona@gmail.com",   Phone = "01099992222" },
            new { Id = "buyer-0000-0000-0000-000000000003", Name = "Aya Mohamed",  Email = "aya@gmail.com",    Phone = "01099993333" },
        };

            foreach (var b in buyers)
            {
                if (await userManager.FindByEmailAsync(b.Email) is not null) continue;

                var user = new User
                {
                    Id = b.Id,
                    Name = b.Name,
                    Email = b.Email,
                    UserName = b.Email,
                    PhoneNumber = b.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                };

                await userManager.CreateAsync(user, "Buyer@123");
                await userManager.AddToRoleAsync(user, AppRoles.Buyer);
            }
        }
    }
}
