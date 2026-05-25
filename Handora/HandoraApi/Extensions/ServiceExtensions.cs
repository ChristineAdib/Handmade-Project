using HandoraApi.Hubs;
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
            services.AddScoped<IChatHubContext, ChatHubContext>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<INotificationHubContext, NotificationHubContext>();
            services.AddSignalR();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();
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
           services.AddScoped<IWishListService, WishListService>();
            return services;
        }
    }
}
