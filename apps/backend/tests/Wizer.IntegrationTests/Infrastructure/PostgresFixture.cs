using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure;

/// <summary>
/// A real PostgreSQL (same image as <c>docker-compose.dev.yml</c>) with every migration applied, shared by
/// the tests of the <see cref="PostgresCollection"/>. Tests do not clean up: each one creates its own data.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AppDbContext CreateContext(IClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, ConnectionString, new AuditInterceptor(clock ?? new SystemClock()));
        return new AppDbContext(options.Options);
    }

    /// <summary>Saves <paramref name="entities"/> in a fresh context and returns the constraint that rejected them.</summary>
    public async Task<string?> SaveAndGetViolatedConstraintAsync(params object[] entities)
    {
        await using var db = CreateContext();
        db.AddRange(entities);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        return Assert.IsType<PostgresException>(exception.InnerException).ConstraintName;
    }

    public async Task SaveAsync(params object[] entities)
    {
        await using var db = CreateContext();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
