using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Infrastructure.Persistence.Seed;
using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Submissions.Entities;
using Wizer.Api.Modules.Teams.Entities;
using Wizer.Api.Modules.Whiteboard.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Time;

namespace Wizer.IntegrationTests.Infrastructure.Persistence;

[Collection(PostgresCollection.Name)]
public class DevSeederTests(PostgresFixture fixture)
{
    private const string Password = "SeedPassword2026";

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesTheDocumentedData()
    {
        await using var db = await NewMigratedDatabaseAsync();

        await NewSeeder(db).SeedAsync(Password);

        Assert.Equal(1, await db.Set<Institution>().CountAsync());
        Assert.Equal(3, await db.Set<Campus>().CountAsync());
        Assert.Equal(1, await db.Set<User>().CountAsync(user => user.PlatformRole == PlatformRole.Admin));
        Assert.Equal(2, await db.Set<User>().CountAsync(user => user.PlatformRole == PlatformRole.Teacher));
        Assert.Equal(DevSeeder.StudentCount, await db.Set<User>().CountAsync(user => user.PlatformRole == PlatformRole.Student));
        Assert.Equal(3, await db.Set<SchoolClass>().CountAsync());
        Assert.Equal(3, await db.Set<ClassMembership>().CountAsync(membership => membership.Role == ClassRole.Teacher));
        Assert.Equal(9, await db.Set<Team>().CountAsync());
        Assert.Equal(9, await db.Set<TeamWhiteboard>().CountAsync());
        Assert.Equal(3, await db.Set<ChatChannel>().CountAsync(channel => channel.Type == ChatChannelType.Class));
        Assert.True(await db.Set<ChatMessage>().AnyAsync());
        Assert.Equal(
            [AssignmentStatus.Draft, AssignmentStatus.Published],
            await db.Set<Assignment>().Select(assignment => assignment.Status).Distinct().OrderBy(status => status).ToListAsync());
        Assert.True(await db.Set<Submission>().AnyAsync(submission => submission.IsLate));
        Assert.True(await db.Set<Submission>().AnyAsync(submission => submission.Status == SubmissionStatus.Graded));
    }

    [Fact]
    public async Task SeedAsync_SeededAccount_VerifiesWithTheConfiguredPassword()
    {
        await using var db = await NewMigratedDatabaseAsync();

        await NewSeeder(db).SeedAsync(Password);

        var admin = await db.Set<User>().SingleAsync(user => user.Email == DevSeeder.AdminEmail);
        Assert.NotNull(admin.EmailVerifiedAt);
        Assert.True(admin.IsActive);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(admin, admin.PasswordHash!, Password));
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicate()
    {
        await using var db = await NewMigratedDatabaseAsync();
        await NewSeeder(db).SeedAsync(Password);
        var users = await db.Set<User>().CountAsync();

        await NewSeeder(db).SeedAsync(Password);

        Assert.Equal(1, await db.Set<Institution>().CountAsync());
        Assert.Equal(users, await db.Set<User>().CountAsync());
    }

    private async Task<AppDbContext> NewMigratedDatabaseAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = $"seed_{Guid.NewGuid():N}",
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, connectionString, new AuditInterceptor(new SystemClock()));
        var db = new AppDbContext(options.Options);
        await db.Database.MigrateAsync();
        return db;
    }

    private static DevSeeder NewSeeder(AppDbContext db) =>
        new(db, new SystemClock(), new UuidV7IdGenerator(), NullLogger<DevSeeder>.Instance);
}
