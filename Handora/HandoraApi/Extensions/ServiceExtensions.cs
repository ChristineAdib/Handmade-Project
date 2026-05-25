using HandoraApi.Services;
using HandoraApplication.Helpers;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.IServices;
using HandoraApplication.Services;
using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries_UOW;

namespace HandoraApi.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<JwtHelper>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ImageHelper>(provider =>
            {
                var env = provider.GetRequiredService<IWebHostEnvironment>();
                return new ImageHelper(env.WebRootPath);
            });
            return services;
        }
    }
}
