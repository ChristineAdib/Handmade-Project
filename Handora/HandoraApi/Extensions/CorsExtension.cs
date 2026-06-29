using Microsoft.Extensions.Configuration;

namespace HandoraApi.Extensions;

public static class CorsExtension
{
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("CorsOrigins").Get<string[]>() 
                      ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:5204" , "https://frontend-handmade-project-dpn3.vercel.app" };

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("development",
                policy =>
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            options.AddPolicy("production",
                policy =>
                {
                    policy
                        .WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }
}
