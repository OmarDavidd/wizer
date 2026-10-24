using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Authorization;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Errors;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.RateLimiting;
using Wizer.Api.Shared.Time;
using Wizer.Api.Shared.Validation;

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
        services.AddValidatorsFromAssemblyContaining<ValidationFilter>(ServiceLifetime.Scoped);
        services.AddWizerRateLimiting();
        return services;
    }

    /// <summary>
    /// Every controller action requires a session unless it is marked <c>[AllowAnonymous]</c>, and its
    /// arguments are validated before it runs. A controller filter, not a fallback policy, so routes
    /// that do not exist still answer 404 instead of 401. JSON only: the contract declares neither
    /// <c>text/plain</c> nor <c>text/json</c> (<c>application/*+json</c> stays for problem+json).
    /// </summary>
    public static IMvcBuilder AddWizerControllers(this IServiceCollection services) =>
        services.AddControllers(options =>
        {
            options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
            options.Filters.Add<ValidationFilter>();
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            foreach (var formatter in options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>())
            {
                formatter.SupportedMediaTypes.Remove("text/json");
            }

            foreach (var formatter in options.InputFormatters.OfType<SystemTextJsonInputFormatter>())
            {
                formatter.SupportedMediaTypes.Remove("text/json");
            }
        });
}
