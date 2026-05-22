using HandoraDomain.Models.AppUser;
using HandoraInfrastructure.Data;
using HandoraInfrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HandoraApi.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task InitialiseDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                // ── 1. Apply any pending migrations automatically ──────────────────
                logger.LogInformation("Applying migrations...");
                await context.Database.MigrateAsync();

                // ── 2. Seed in order (respect FK dependencies) ────────────────────
                logger.LogInformation("Seeding roles and users...");
                await UserSeeder.SeedAsync(userManager, roleManager);

                logger.LogInformation("Seeding delivery methods...");
                await DeliveryMethodSeeder.SeedAsync(context);

                logger.LogInformation("Seeding categories...");
                await CategorySeeder.SeedAsync(context);

                logger.LogInformation("Seeding shops...");
                await ShopSeeder.SeedAsync(context);

                logger.LogInformation("Seeding products...");
                await ProductSeeder.SeedAsync(context);

                logger.LogInformation("Seeding coupons...");
                await CouponSeeder.SeedAsync(context);

                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                throw;
            }
        }
        }
}
