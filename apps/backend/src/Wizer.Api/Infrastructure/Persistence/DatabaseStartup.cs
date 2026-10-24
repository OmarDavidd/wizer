using Microsoft.EntityFrameworkCore;
using Wizer.Api.Infrastructure.Persistence.Seed;

namespace Wizer.Api.Infrastructure.Persistence;

public static class DatabaseStartup
{
    public const string ApplyMigrationsKey = "Database:ApplyMigrationsOnStartup";

    /// <summary>
    /// Development only: applies pending migrations when <c>Database:ApplyMigrationsOnStartup=true</c> and
    /// loads the development data when <c>Seed:Enabled=true</c>. Production never migrates from the API;
    /// the <c>migrator</c> service does it (<c>.ia/database.md</c>).
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var applyMigrations = app.Configuration.GetValue<bool>(ApplyMigrationsKey);
        var seed = app.Configuration.GetValue<bool>(DevSeeder.EnabledKey);
        if (!applyMigrations && !seed)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        if (applyMigrations)
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }

        if (seed)
        {
            var password = app.Configuration[DevSeeder.PasswordKey];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException($"'{DevSeeder.PasswordKey}' is required when '{DevSeeder.EnabledKey}' is true.");
            }

            await scope.ServiceProvider.GetRequiredService<DevSeeder>().SeedAsync(password);
        }
    }
}
