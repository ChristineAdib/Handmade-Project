namespace HandoraApplication;

using HandoraApplication.IServices;
using HandoraApplication.Mappers;
using HandoraApplication.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleApplicationDependences
{
    public static IServiceCollection AddReposetoriesServices(this IServiceCollection services)
    {
        MapsterSettings.Configure();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<IEscrowService, EscrowService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISellerService, SellerService>();
        services.AddScoped<IFollowService, FollowService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}