namespace HandoraApi.Extensions;

public static class CorsExtension
{
    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("development",
                policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            options.AddPolicy("production",
                policy =>
                {
                    policy
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });
    }
}
