using Wizer.Api.Modules.Campuses.Interfaces;
using Wizer.Api.Modules.Campuses.Repositories;
using Wizer.Api.Modules.Campuses.Services;

namespace Wizer.Api.Modules.Campuses;

public static class DependencyInjection
{
    public static IServiceCollection AddCampusesModule(this IServiceCollection services)
    {
        services.AddScoped<ICampusModuleService, CampusModuleService>();
        services.AddScoped<ICampusRepository, CampusRepository>();
        return services;
    }
}
