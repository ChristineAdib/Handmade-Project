using HandoraApplication.Helpers.AuthHelper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
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
        });
    }
}
