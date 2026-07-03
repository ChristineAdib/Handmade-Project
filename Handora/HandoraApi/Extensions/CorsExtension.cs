using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace HandoraApi.Extensions;

public static class CorsExtension
{
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("CorsOrigins").Get<string[]>() 
                                ?? Array.Empty<string>();

        var allowedOrigins = new HashSet<string>(configuredOrigins, StringComparer.OrdinalIgnoreCase)
        {
            "http://localhost:4200",
            "http://127.0.0.1:4200",
            "https://frontend-handmade-project-dpn3.vercel.app"
        };

        var frontendUrl = configuration["FrontendUrl"];
        if (!string.IsNullOrEmpty(frontendUrl))
        {
            allowedOrigins.Add(frontendUrl);
        }

        services.AddCors(options =>
        {
            var policyAction = new Action<Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder>(policy =>
            {
                policy
                    .SetIsOriginAllowed(origin => 
                    {
                        if (allowedOrigins.Contains(origin))
                            return true;

                        // Allow Vercel preview deployment URLs (e.g., https://*.vercel.app)
                        if (origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && 
                            origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        return false;
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddDefaultPolicy(policyAction);
            options.AddPolicy("development", policyAction);
            options.AddPolicy("production", policyAction);
        });
    }
}
