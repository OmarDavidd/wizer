using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Teams.Entities;
using Wizer.Api.Shared.Auth;

namespace Wizer.IntegrationTests.Infrastructure;

/// <summary>
/// Builds and saves the rows schema tests need, with unique values so tests sharing the database never collide.
/// </summary>
public sealed class SchemaBuilder(PostgresFixture fixture)
{
    private static readonly Random JoinCodeRandom = new();

    public Guid InstitutionId { get; } = Guid.CreateVersion7();

    public async Task<SchemaBuilder> WithInstitutionAsync()
    {
        await fixture.SaveAsync(new Institution { Id = InstitutionId, Name = $"Institution {InstitutionId}" });
        return this;
    }

    public async Task<User> UserAsync(PlatformRole role = PlatformRole.Student)
    {
        var user = NewUser(role);
        await fixture.SaveAsync(user);
        return user;
    }

    public User NewUser(PlatformRole role = PlatformRole.Student) => new()
    {
        InstitutionId = InstitutionId,
        FullName = "Test User",
        Email = $"{Guid.CreateVersion7():N}@test.local",
        PlatformRole = role,
        IsActive = true,
    };

    public async Task<SchoolClass> ClassAsync(string? joinCode = null)
    {
        var schoolClass = NewClass(joinCode);
        await fixture.SaveAsync(schoolClass);
        return schoolClass;
    }

    public SchoolClass NewClass(string? joinCode = null) => new()
    {
        InstitutionId = InstitutionId,
        Name = "Test class",
        JoinCode = joinCode ?? NewJoinCode(),
    };

    public async Task<Team> TeamAsync(Guid classId, string? name = null)
    {
        var team = new Team { ClassId = classId, Name = name ?? $"Team {Guid.CreateVersion7():N}", MaxSize = 5 };
        await fixture.SaveAsync(team);
        return team;
    }

    public async Task<Assignment> AssignmentAsync(Guid classId, Guid teacherId, AssignmentMode mode = AssignmentMode.Individual)
    {
        var assignment = new Assignment
        {
            ClassId = classId,
            Title = "Test assignment",
            Instructions = "Do it",
            DueAt = DateTimeOffset.UtcNow.AddDays(7),
            Mode = mode,
            GradingType = GradingType.Points,
            MaxPoints = 10,
            Status = AssignmentStatus.Published,
            CreatedById = teacherId,
        };
        await fixture.SaveAsync(assignment);
        return assignment;
    }

    public static string NewJoinCode()
    {
        lock (JoinCodeRandom)
        {
            return new string(JoinCodeRandom.GetItems(SchoolClass.JoinCodeAlphabet.AsSpan(), SchoolClass.JoinCodeLength));
        }
    }
}
