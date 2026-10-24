using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Teams.Entities;

/// <summary>
/// A student in a team. <see cref="ClassId"/> repeats the team's class so the database can enforce one team
/// per student and class (BR-TEAM-02); a composite foreign key keeps it equal to the team's class.
/// </summary>
public class TeamMember : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public Guid ClassId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
