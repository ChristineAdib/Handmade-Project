namespace HandoraInfrastructure;

using HandoraDomain.Interfaces;
using HandoraInfrastructure.Repositries_UOW;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleInfrastructureDependences
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection service)
    {
        service.AddTransient<IUnitOfWork, UnitOfWork>();
        return service;
    }
}
