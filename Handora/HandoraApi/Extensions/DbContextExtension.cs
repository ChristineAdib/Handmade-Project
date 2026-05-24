using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraApi.Extensions;

public static class DbContextExtension
{
    public static void ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration. Please ensure it is defined in appsettings.Development.json or appsettings.json.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}