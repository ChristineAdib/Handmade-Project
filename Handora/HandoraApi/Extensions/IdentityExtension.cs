using HandoraDomain.Models.AppUser;
using HandoraInfrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace HandoraApi.Extensions;

public static class IdentityExtension
{
    public static void ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole>(options =>
        {
            // Configure password options
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 0;

            options.User.RequireUniqueEmail = true;

        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    }
}