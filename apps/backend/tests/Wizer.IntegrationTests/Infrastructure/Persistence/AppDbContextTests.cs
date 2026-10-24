using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure.Persistence;

[Collection(PostgresCollection.Name)]
public class AppDbContextTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_AppliedToEmptyDatabase_MatchTheModel()
    {
        await using var db = fixture.CreateContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "The model changed: add a migration.");
    }

    [Fact]
    public async Task SaveChanges_NewEntity_StampsCreatedAndUpdatedAtFromClock()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var now = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        var user = builder.NewUser();

        await using (var db = fixture.CreateContext(new FixedClock(now)))
        {
            db.Add(user);
            await db.SaveChangesAsync();
        }

        await using var read = fixture.CreateContext();
        var saved = await read.Set<User>().SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(now, saved.CreatedAt);
        Assert.Equal(now, saved.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_ModifiedEntity_RefreshesUpdatedAtOnly()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var created = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var edited = created.AddDays(3);
        var user = builder.NewUser();
        await using (var db = fixture.CreateContext(new FixedClock(created)))
        {
            db.Add(user);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext(new FixedClock(edited)))
        {
            var tracked = await db.Set<User>().SingleAsync(candidate => candidate.Id == user.Id);
            tracked.FullName = "Renamed";
            await db.SaveChangesAsync();
        }

        await using var read = fixture.CreateContext();
        var saved = await read.Set<User>().SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(created, saved.CreatedAt);
        Assert.Equal(edited, saved.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_EntityWithoutId_GetsUuidV7()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();

        var user = await builder.UserAsync();

        Assert.Equal(7, user.Id.Version);
    }

    [Fact]
    public async Task SaveChanges_Enum_IsStoredAsSnakeCaseText()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var user = await builder.UserAsync(PlatformRole.Teacher);

        await using var db = fixture.CreateContext();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT platform_role AS \"Value\" FROM users WHERE id = {user.Id}")
            .SingleAsync();

        Assert.Equal("teacher", stored);
    }

    [Fact]
    public async Task Insert_EnumValueOutsideTheSet_ViolatesCheckConstraint()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var user = await builder.UserAsync();

        await using var db = fixture.CreateContext();
        var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE users SET platform_role = 'guest' WHERE id = {user.Id}"));

        Assert.Equal("ck_users_platform_role", exception.ConstraintName);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
