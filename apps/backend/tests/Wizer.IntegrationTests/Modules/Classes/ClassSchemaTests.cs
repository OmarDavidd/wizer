using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Classes;

[Collection(PostgresCollection.Name)]
public class ClassSchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertMembership_SecondTeacherInClass_ViolatesSingleTeacher()
    {
        // RN-02 / BR-CLS-01: one teacher per class.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var first = await builder.UserAsync(PlatformRole.Teacher);
        var second = await builder.UserAsync(PlatformRole.Teacher);
        await fixture.SaveAsync(new ClassMembership { ClassId = schoolClass.Id, UserId = first.Id, Role = ClassRole.Teacher });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new ClassMembership { ClassId = schoolClass.Id, UserId = second.Id, Role = ClassRole.Teacher });

        Assert.Equal("ux_class_memberships_class_id_teacher", constraint);
    }

    [Fact]
    public async Task InsertMembership_SameUserTwice_ViolatesUniqueMembership()
    {
        // BR-CLS-05: joining a class twice.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var student = await builder.UserAsync();
        await fixture.SaveAsync(new ClassMembership { ClassId = schoolClass.Id, UserId = student.Id, Role = ClassRole.Student });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new ClassMembership { ClassId = schoolClass.Id, UserId = student.Id, Role = ClassRole.Student });

        Assert.Equal("ux_class_memberships_class_id_user_id", constraint);
    }

    [Fact]
    public async Task InsertClass_JoinCodeOfAnActiveClass_ViolatesUniqueJoinCode()
    {
        // BR-CLS-02: unique among active classes.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var existing = await builder.ClassAsync();

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(builder.NewClass(existing.JoinCode));

        Assert.Equal("ux_school_classes_join_code", constraint);
    }

    [Fact]
    public async Task InsertClass_JoinCodeOfAnArchivedClass_IsAllowed()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var archived = builder.NewClass();
        archived.ArchivedAt = DateTimeOffset.UtcNow;
        await fixture.SaveAsync(archived);

        await fixture.SaveAsync(builder.NewClass(archived.JoinCode));
    }

    [Fact]
    public async Task InsertClass_JoinCodeWithAmbiguousCharacters_ViolatesCheck()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(builder.NewClass("ABC10O"));

        Assert.Equal("ck_school_classes_join_code", constraint);
    }

    [Fact]
    public async Task Query_SoftDeletedClass_IsHiddenByDefault()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var deleted = builder.NewClass();
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        await fixture.SaveAsync(deleted);

        await using var db = fixture.CreateContext();

        Assert.False(await db.Set<SchoolClass>().AnyAsync(schoolClass => schoolClass.Id == deleted.Id));
        Assert.True(await db.Set<SchoolClass>().IgnoreQueryFilters().AnyAsync(schoolClass => schoolClass.Id == deleted.Id));
    }
}
