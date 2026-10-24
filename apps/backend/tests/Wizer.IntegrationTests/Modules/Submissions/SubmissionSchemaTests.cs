using Wizer.Api.Modules.Assignments.Entities;
using Wizer.Api.Modules.Submissions.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Submissions;

[Collection(PostgresCollection.Name)]
public class SubmissionSchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertSubmission_SecondForSameStudentAndAssignment_ViolatesUnique()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var teacher = await builder.UserAsync(PlatformRole.Teacher);
        var student = await builder.UserAsync();
        var assignment = await builder.AssignmentAsync(schoolClass.Id, teacher.Id);
        await fixture.SaveAsync(new Submission { AssignmentId = assignment.Id, StudentId = student.Id });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new Submission { AssignmentId = assignment.Id, StudentId = student.Id });

        Assert.Equal("ux_submissions_assignment_id_student_id", constraint);
    }

    [Fact]
    public async Task InsertSubmission_SecondForSameTeamAndAssignment_ViolatesUnique()
    {
        // BR-SUB-07: one submission per team.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var teacher = await builder.UserAsync(PlatformRole.Teacher);
        var team = await builder.TeamAsync(schoolClass.Id);
        var assignment = await builder.AssignmentAsync(schoolClass.Id, teacher.Id, AssignmentMode.Team);
        await fixture.SaveAsync(new Submission { AssignmentId = assignment.Id, TeamId = team.Id });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new Submission { AssignmentId = assignment.Id, TeamId = team.Id });

        Assert.Equal("ux_submissions_assignment_id_team_id", constraint);
    }

    [Fact]
    public async Task InsertSubmission_WithStudentAndTeam_ViolatesOwnerCheck()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var teacher = await builder.UserAsync(PlatformRole.Teacher);
        var student = await builder.UserAsync();
        var team = await builder.TeamAsync(schoolClass.Id);
        var assignment = await builder.AssignmentAsync(schoolClass.Id, teacher.Id);

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new Submission { AssignmentId = assignment.Id, StudentId = student.Id, TeamId = team.Id });

        Assert.Equal("ck_submissions_owner", constraint);
    }

    [Fact]
    public async Task InsertAssignment_PassFailWithMaxPoints_ViolatesCheck()
    {
        // BR-ASG-01: max points only for points grading.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var teacher = await builder.UserAsync(PlatformRole.Teacher);

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(new Assignment
        {
            ClassId = schoolClass.Id,
            Title = "Pass/fail",
            Instructions = "Do it",
            DueAt = DateTimeOffset.UtcNow.AddDays(1),
            GradingType = GradingType.PassFail,
            MaxPoints = 10,
            CreatedById = teacher.Id,
        });

        Assert.Equal("ck_assignments_max_points", constraint);
    }
}
