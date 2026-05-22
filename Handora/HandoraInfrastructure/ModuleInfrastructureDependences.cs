namespace HandoraInfrastructure;

using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries;
using HandoraInfrastructure.Repositries_UOW;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleInfrastructureDependences
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service)
    {
        service.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        service.AddScoped<IUnitOfWork, UnitOfWork>();
        return service;
    }
}
