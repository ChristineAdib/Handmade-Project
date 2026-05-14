using HandoraInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HandoraApi.Extensions;

public static class DbContextExtension
{
    public static void ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}