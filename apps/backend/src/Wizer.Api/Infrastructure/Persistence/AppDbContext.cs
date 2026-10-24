using Microsoft.EntityFrameworkCore;

namespace Wizer.Api.Infrastructure.Persistence;

/// <summary>
/// The only DbContext. Each module owns its entities and <c>IEntityTypeConfiguration</c> classes;
/// repositories reach their tables with <c>Set&lt;T&gt;()</c>. Guid keys left empty get a UUID v7
/// from Npgsql on insert; a Service that needs the id earlier uses <c>IIdGenerator</c>.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Shared by the runtime registration, the design-time factory and the tests.</summary>
    public static DbContextOptionsBuilder Configure(
        DbContextOptionsBuilder options,
        string connectionString,
        AuditInterceptor auditInterceptor) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
