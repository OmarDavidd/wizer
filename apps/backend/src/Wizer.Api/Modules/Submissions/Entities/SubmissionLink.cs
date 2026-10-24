using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Submissions.Entities;

/// <summary>A link in a submission, for example a video (BR-SUB-01, BR-FILE-03). Up to 5 per submission.</summary>
public class SubmissionLink : IHasCreatedAt
{
    public const int MaxUrlLength = 2048;

    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public required string Url { get; set; }

    public string? Title { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
