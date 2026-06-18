namespace HandoraInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries;
using HandoraInfrastructure.Repositries_UOW;
using HandoraInfrastructure.Settings;
using HandoraInfrastructure.Services;
using Microsoft.Extensions.Configuration;

public static class ModuleInfrastructureDependences
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service , IConfiguration configuration)
    {
        service.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        service.AddScoped<IProductRepository, ProductRepository>();
        service.AddScoped<IOrderRepository, OrderRepository>();
        service.AddScoped<ICouponRepository, CouponRepository>();
        service.AddScoped<IShopRepository, ShopRepository>();
        service.AddScoped<IUnitOfWork, UnitOfWork>();
        service.Configure<PaymobSettings>(
        configuration.GetSection("Paymob"));
        service.AddScoped<IOtpRepository, OtpRepository>();
        service.AddScoped<IUserStatsRepository, UserStatsRepository>();
        
        service.AddScoped<IAiReviewService, GeminiAiService>();
        
        return service;
    }
}
