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
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISellerService, SellerService>();
        services.AddScoped<IFollowService, FollowService>();
        return services;
    }
}
