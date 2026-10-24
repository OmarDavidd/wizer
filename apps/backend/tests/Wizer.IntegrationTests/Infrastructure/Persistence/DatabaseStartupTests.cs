using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Wizer.Api.Infrastructure.Persistence;

namespace Wizer.IntegrationTests.Infrastructure.Persistence;

[Collection(PostgresCollection.Name)]
public class DatabaseStartupTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Startup_ApplyMigrationsEnabledInDevelopment_CreatesTheSchema()
    {
        var connectionString = NewDatabase();

        await StartApiAsync(connectionString, applyMigrations: true);

        Assert.Equal(1L, await CountAppliedMigrationsAsync(connectionString, "InitialSchema"));
    }

    [Fact]
    public async Task Startup_ApplyMigrationsDisabled_LeavesTheDatabaseUntouched()
    {
        var connectionString = NewDatabase();

        await StartApiAsync(connectionString, applyMigrations: false);

        Assert.False(await DatabaseExistsAsync(connectionString));
    }

    private string NewDatabase() => new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
    {
        Database = $"startup_{Guid.NewGuid():N}",
    }.ConnectionString;

    private static async Task StartApiAsync(string connectionString, bool applyMigrations)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .UseSetting("ConnectionStrings:Wizer", connectionString)
            .UseSetting(DatabaseStartup.ApplyMigrationsKey, applyMigrations.ToString()));

        // Creating the client builds and starts the host, which runs the startup migration.
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
    }

    private static async Task<long> CountAppliedMigrationsAsync(string connectionString, string name)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {AppDbContext.MigrationsHistoryTable} WHERE migration_id LIKE '%' || @name",
            connection);
        command.Parameters.AddWithValue("name", name);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> DatabaseExistsAsync(string connectionString)
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", connection);
        command.Parameters.AddWithValue("name", database!);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
