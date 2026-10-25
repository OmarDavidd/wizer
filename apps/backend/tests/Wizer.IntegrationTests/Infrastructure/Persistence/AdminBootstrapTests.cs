using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Infrastructure.Persistence.Seed;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure.Persistence;

/// <summary>The first production administrator, seeded from SEED_ADMIN_EMAIL / SEED_ADMIN_PASSWORD.</summary>
[Collection(PostgresCollection.Name)]
public class AdminBootstrapTests(PostgresFixture fixture)
{
    private const string Email = "admin@novauniversitas.test";
    private const string Password = "primeradmin2026";

    [Fact]
    public async Task Startup_WithSeedAdmin_CreatesAVerifiedActiveAdminInTheExistingInstitution()
    {
        var connectionString = await MigratedDatabaseAsync();
        var institutionId = await AddInstitutionAsync(connectionString, "NovaUniversitas");

        await using var factory = Start(connectionString, (AdminBootstrapper.EmailKey, $"  {Email.ToUpperInvariant()} "));
        using var client = factory.CreateClient();

        await using var db = Context(connectionString);
        var admin = await db.Set<User>().SingleAsync();
        Assert.Equal(Email, admin.Email);
        Assert.Equal(PlatformRole.Admin, admin.PlatformRole);
        Assert.Equal(institutionId, admin.InstitutionId);
        Assert.Null(admin.CampusId);
        Assert.True(admin.IsActive);
        Assert.NotNull(admin.EmailVerifiedAt);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Startup_Twice_CreatesTheAdminOnlyOnce()
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");

        await StartAndStopAsync(connectionString);
        await StartAndStopAsync(connectionString);

        await using var db = Context(connectionString);
        Assert.Equal(1, await db.Set<User>().CountAsync());
    }

    [Fact]
    public async Task Startup_WhenAnAdminAlreadyExists_DoesNothing()
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");
        await StartAndStopAsync(connectionString, (AdminBootstrapper.EmailKey, "otro-admin@novauniversitas.test"));

        await StartAndStopAsync(connectionString);

        await using var db = Context(connectionString);
        Assert.Equal("otro-admin@novauniversitas.test", (await db.Set<User>().SingleAsync()).Email);
    }

    [Fact]
    public async Task Startup_WithoutInstitution_CreatesItFromTheConfiguredName()
    {
        var connectionString = await MigratedDatabaseAsync();

        await StartAndStopAsync(connectionString, (AdminBootstrapper.InstitutionNameKey, "NovaUniversitas"));

        await using var db = Context(connectionString);
        var institution = await db.Set<Institution>().SingleAsync();
        Assert.Equal("NovaUniversitas", institution.Name);
        Assert.Equal(institution.Id, (await db.Set<User>().SingleAsync()).InstitutionId);
    }

    [Fact]
    public async Task Startup_WithoutInstitutionNorName_FailsWithAClearMessage()
    {
        var connectionString = await MigratedDatabaseAsync();

        var exception = await StartupFailureAsync(connectionString);

        Assert.Contains(AdminBootstrapper.InstitutionNameKey, exception.Message);
    }

    [Fact]
    public async Task Startup_WeakPassword_FailsWithAClearMessage()
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");

        var exception = await StartupFailureAsync(connectionString, (AdminBootstrapper.PasswordKey, "corta"));

        Assert.Contains(AdminBootstrapper.PasswordKey, exception.Message);
    }

    [Fact]
    public async Task Startup_EmailOfAnExistingNonAdmin_FailsInsteadOfPromotingIt()
    {
        var connectionString = await MigratedDatabaseAsync();
        var institutionId = await AddInstitutionAsync(connectionString, "NovaUniversitas");
        await using (var db = Context(connectionString))
        {
            db.Add(new User
            {
                InstitutionId = institutionId,
                FullName = "Alumno",
                Email = Email,
                PlatformRole = PlatformRole.Student,
                IsActive = true,
            });
            await db.SaveChangesAsync();
        }

        var exception = await StartupFailureAsync(connectionString);

        Assert.Contains(AdminBootstrapper.EmailKey, exception.Message);
    }

    [Theory]
    [InlineData(AdminBootstrapper.EmailKey)]
    [InlineData(AdminBootstrapper.PasswordKey)]
    public async Task Startup_WithOnlyOneOfEmailAndPassword_FailsWithAClearMessage(string missing)
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");

        var exception = await StartupFailureAsync(connectionString, (missing, ""));

        Assert.Contains(missing, exception.Message);
    }

    [Fact]
    public async Task Startup_AfterTheFirstAdmin_StartsEvenWithOnlyTheEmailLeft()
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");
        await StartAndStopAsync(connectionString);

        await StartAndStopAsync(connectionString, (AdminBootstrapper.PasswordKey, ""));

        await using var db = Context(connectionString);
        Assert.Equal(1, await db.Set<User>().CountAsync());
    }

    [Fact]
    public async Task Startup_WithoutSeedSettings_DoesNotTouchTheDatabase()
    {
        var connectionString = await MigratedDatabaseAsync();
        await AddInstitutionAsync(connectionString, "NovaUniversitas");

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("ConnectionStrings:Wizer", connectionString)
            .UseSetting("Jwt:SigningKey", WizerApiFactory.SigningKey));
        using var client = factory.CreateClient();

        await using var db = Context(connectionString);
        Assert.False(await db.Set<User>().AnyAsync());
    }

    private static WebApplicationFactory<Program> Start(string connectionString, params (string Key, string Value)[] overrides)
    {
        var settings = new Dictionary<string, string>
        {
            [AdminBootstrapper.EmailKey] = Email,
            [AdminBootstrapper.PasswordKey] = Password,
        };
        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Wizer", connectionString);
            builder.UseSetting("Jwt:SigningKey", WizerApiFactory.SigningKey);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
    }

    private static async Task StartAndStopAsync(string connectionString, params (string Key, string Value)[] overrides)
    {
        await using var factory = Start(connectionString, overrides);
        using var client = factory.CreateClient();
    }

    private static async Task<Exception> StartupFailureAsync(string connectionString, params (string Key, string Value)[] overrides)
    {
        await using var factory = Start(connectionString, overrides);
        return await Assert.ThrowsAnyAsync<Exception>(() => Task.Run(() => factory.CreateClient()));
    }

    private async Task<string> MigratedDatabaseAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"bootstrap_{Guid.NewGuid():N}",
        }.ConnectionString;
        await using var db = Context(connectionString);
        await db.Database.MigrateAsync();
        return connectionString;
    }

    private static async Task<Guid> AddInstitutionAsync(string connectionString, string name)
    {
        await using var db = Context(connectionString);
        var institution = new Institution { Id = Guid.CreateVersion7(), Name = name };
        db.Add(institution);
        await db.SaveChangesAsync();
        return institution.Id;
    }

    private static AppDbContext Context(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, connectionString, new AuditInterceptor(new SystemClock()));
        return new AppDbContext(options.Options);
    }
}
