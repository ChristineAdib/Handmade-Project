namespace HandoraInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries;
using HandoraInfrastructure.Repositries_UOW;
using HandoraInfrastructure.Settings;
using Microsoft.Extensions.Configuration;

public static class ModuleInfrastructureDependences
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service , IConfiguration configuration)
    {
        service.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        service.AddScoped<IProductRepository, ProductRepository>();
        service.AddScoped<IUnitOfWork, UnitOfWork>();
        service.Configure<PaymobSettings>(
        configuration.GetSection("Paymob"));
        return service;
    }
}
