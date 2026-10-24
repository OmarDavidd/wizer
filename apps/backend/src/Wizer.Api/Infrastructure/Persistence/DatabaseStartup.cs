using Microsoft.EntityFrameworkCore;

namespace Wizer.Api.Infrastructure.Persistence;

public static class DatabaseStartup
{
    public const string ApplyMigrationsKey = "Database:ApplyMigrationsOnStartup";

    /// <summary>
    /// Development only: applies pending migrations when <c>Database:ApplyMigrationsOnStartup=true</c>.
    /// Production never migrates from the API; the <c>migrator</c> service does it (<c>.ia/database.md</c>).
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue<bool>(ApplyMigrationsKey))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
