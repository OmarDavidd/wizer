using Microsoft.AspNetCore.Identity;
using Wizer.Api.Modules.Identity.Classes;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Identity.Interfaces;
using Wizer.Api.Modules.Identity.Repositories;
using Wizer.Api.Modules.Identity.Services;

namespace Wizer.Api.Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<IGoogleSignInService, GoogleSignInService>();
        services.AddScoped<InvitationService>();
        services.AddScoped<IInvitationService>(provider => provider.GetRequiredService<InvitationService>());
        services.AddScoped<IIdentityModuleService>(provider => provider.GetRequiredService<InvitationService>());
        services.AddScoped<IInvitationTokenRepository, InvitationTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddSingleton<LoginAttemptLimiter>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationCodeSender, VerificationCodeSender>();
        services.AddScoped<ISessionIssuer, SessionIssuer>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        return services;
    }
}
