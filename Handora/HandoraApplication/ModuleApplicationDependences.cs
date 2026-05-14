namespace HandoraApplication;

using HandoraApplication.Mappers;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleApplicationDependences
{
    public static IServiceCollection AddReposetoriesServices(this IServiceCollection services)
    {
        MapsterSettings.Configure();
        return services;
    }
}
