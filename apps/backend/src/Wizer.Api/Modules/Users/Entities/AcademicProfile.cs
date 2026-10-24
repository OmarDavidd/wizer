using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Users.Entities;

/// <summary>
/// One-to-one with <c>users</c>; the user id is the key. Students fill career, semester and enrollment
/// number; teachers the professional title. Enrollment number and title are read-only for the user (PERF-03).
/// </summary>
public class AcademicProfile : IHasCreatedAt, IHasUpdatedAt
{
    public Guid UserId { get; set; }

    public string? Career { get; set; }

    public short? Semester { get; set; }

    public string? EnrollmentNumber { get; set; }

    public string? ProfessionalTitle { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
