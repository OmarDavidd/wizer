using Wizer.Api.Modules.Teams.Entities;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Teams;

[Collection(PostgresCollection.Name)]
public class TeamSchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertMember_StudentAlreadyInAnotherTeamOfTheClass_ViolatesOneTeamPerClass()
    {
        // BR-TEAM-02: one team per student and class.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var teamA = await builder.TeamAsync(schoolClass.Id);
        var teamB = await builder.TeamAsync(schoolClass.Id);
        var student = await builder.UserAsync();
        await fixture.SaveAsync(new TeamMember { TeamId = teamA.Id, ClassId = schoolClass.Id, UserId = student.Id });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new TeamMember { TeamId = teamB.Id, ClassId = schoolClass.Id, UserId = student.Id });

        Assert.Equal("ux_team_members_class_id_user_id", constraint);
    }

    [Fact]
    public async Task InsertMember_ClassDifferentFromTheTeamsClass_ViolatesForeignKey()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var otherClass = await builder.ClassAsync();
        var team = await builder.TeamAsync(schoolClass.Id);
        var student = await builder.UserAsync();

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new TeamMember { TeamId = team.Id, ClassId = otherClass.Id, UserId = student.Id });

        Assert.Equal("fk_team_members_teams_team_id_class_id", constraint);
    }

    [Fact]
    public async Task InsertTeam_DuplicatedNameInClass_ViolatesUniqueName()
    {
        // BR-TEAM-01: unique name inside the class.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        await builder.TeamAsync(schoolClass.Id, "Equipo 1");

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new Team { ClassId = schoolClass.Id, Name = "Equipo 1", MaxSize = 4 });

        Assert.Equal("ux_teams_class_id_name", constraint);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public async Task InsertTeam_MaxSizeOutOfRange_ViolatesCheck(short maxSize)
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new Team { ClassId = schoolClass.Id, Name = "Big", MaxSize = maxSize });

        Assert.Equal("ck_teams_max_size", constraint);
    }
}
