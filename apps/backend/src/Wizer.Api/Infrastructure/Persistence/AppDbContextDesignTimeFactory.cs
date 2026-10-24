using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> so adding a migration needs neither the host nor a database.
/// The connection string is a placeholder: <c>migrations add</c> never connects.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, "Host=localhost;Database=wizer_design", new AuditInterceptor(new SystemClock()));
        return new AppDbContext(options.Options);
    }
}
