using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wizer.Api.Infrastructure.Email;
using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure;

/// <summary>
/// The API against the shared Testcontainers PostgreSQL, with captured emails and a controllable clock.
/// Tests create their own unique data instead of cleaning up (<see cref="PostgresFixture"/>).
/// </summary>
public sealed class WizerApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-tests-signing-key-with-more-than-32-bytes";

    public InMemoryEmailSender Emails { get; } = new();

    public TestClock Clock { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Wizer", postgres.ConnectionString);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }
}
