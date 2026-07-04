using HandoraApplication.Helpers.AuthHelper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using HandoraDomain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HandoraApi.Extensions;

public static class AuthenticationExtension
{
    public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "SmartScheme";
            options.DefaultAuthenticateScheme = "SmartScheme";
            options.DefaultChallengeScheme = "SmartScheme";
        })
        .AddPolicyScheme("SmartScheme", "SmartScheme", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var path = context.Request.Path.Value ?? "";
                if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) || 
                    path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                return Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
            };
        })
        .AddJwtBearer(o =>
        {
            o.IncludeErrorDetails = true;
            o.RequireHttpsMetadata = false;
            o.SaveToken = false;

            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidIssuer = configuration[$"{JwtOptions.SectionName}:Issuer"],
                ValidAudience = configuration[$"{JwtOptions.SectionName}:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration[$"{JwtOptions.SectionName}:Key"]!)),
                ClockSkew = TimeSpan.Zero
            };

            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // 1- ???? ?????? ?? HttpOnly Cookie
                    if (context.Request.Cookies.TryGetValue("accessToken", out var cookieToken) &&
                        !string.IsNullOrWhiteSpace(cookieToken))
                    {
                        context.Token = cookieToken;
                        return Task.CompletedTask;
                    }

                    // 2- ??? SignalR ??? ??
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/chat") ||
                         path.StartsWithSegments("/hubs/notifications")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },

                OnTokenValidated = async context =>
                {
                    var authRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IAuthRepository>();

                    var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? context.Principal?.FindFirstValue("sub")
                                 ?? context.Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

                    if (string.IsNullOrEmpty(userId) || await authRepository.GetByIdAsync(userId) == null)
                    {
                        context.Fail("User does not exist or has been deleted.");
                        return;
                    }

                    // Ensure NameIdentifier is present on the identity for downstream controllers/middleware
                    if (context.Principal?.Identity is ClaimsIdentity identity && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
                    }
                }
            };
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });
    }
}
