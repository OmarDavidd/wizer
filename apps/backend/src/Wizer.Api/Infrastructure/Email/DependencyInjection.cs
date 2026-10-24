namespace Wizer.Api.Infrastructure.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddWizerEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var provider = configuration[$"{EmailOptions.SectionName}:{nameof(EmailOptions.Provider)}"] ?? "smtp";
        switch (provider.Trim().ToLowerInvariant())
        {
            case "smtp":
                services.AddSingleton<IEmailSender, SmtpEmailSender>();
                break;
            case "resend":
                services.AddHttpClient<ResendEmailSender>(client => client.Timeout = TimeSpan.FromSeconds(10));
                services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<ResendEmailSender>());
                break;
            default:
                throw new InvalidOperationException($"Email:Provider '{provider}' is not supported (smtp or resend).");
        }

        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailQueue>(provider => provider.GetRequiredService<EmailQueue>());
        services.AddHostedService<EmailDispatcher>();
        return services;
    }
}
