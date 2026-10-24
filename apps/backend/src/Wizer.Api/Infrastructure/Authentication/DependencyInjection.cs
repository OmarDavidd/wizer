using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Infrastructure.Authentication;

public static class DependencyInjection
{
    /// <summary>
    /// JWT read only from the <c>wz_access</c> cookie (never from the Authorization header), with the
    /// original claim names. The key is resolved when the first request is authenticated, so building
    /// the OpenAPI document needs no secret.
    /// </summary>
    public static IServiceCollection AddWizerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtSigningKey>();
        services.AddSingleton<IAccessTokenIssuer, AccessTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue(AuthCookies.Access, out var token) && token.Length > 0)
                    {
                        context.Token = token;
                    }
                    else
                    {
                        context.NoResult();
                    }

                    return Task.CompletedTask;
                },
            };
        });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSigningKey>((options, signingKey) => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = AccessTokenIssuer.Issuer,
                ValidAudience = AccessTokenIssuer.Audience,
                IssuerSigningKey = signingKey.Key,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = WizerClaimTypes.Subject,
                RoleClaimType = WizerClaimTypes.Role,
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Teacher, policy => policy.RequireClaim(WizerClaimTypes.Role, PlatformRole.Teacher.ToClaimValue()))
            .AddPolicy(AuthPolicies.Admin, policy => policy.RequireClaim(WizerClaimTypes.Role, PlatformRole.Admin.ToClaimValue()));

        return services;
    }
}
