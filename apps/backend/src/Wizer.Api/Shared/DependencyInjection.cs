using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Errors;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddWizerProblemDetails();
        return services;
    }
}
