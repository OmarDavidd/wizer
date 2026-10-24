using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Files.Entities;

/// <summary>Who may see a file depends on its context (<c>.ia/files.md</c> §Contextos).</summary>
public enum FileContextType
{
    /// <summary>Context id: the class.</summary>
    ClassMaterial,

    /// <summary>Context id: the assignment.</summary>
    AssignmentMaterial,

    /// <summary>Context id: the submission.</summary>
    Submission,

    /// <summary>Context id: the chat channel. <see cref="StoredFile.ChatMessageId"/> links the message.</summary>
    ChatAttachment,

    /// <summary>Context id: the user. The only context without a class.</summary>
    Avatar,
}

public enum FileStatus
{
    Uploading,
    PendingScan,
    Available,
    Rejected,
    ScanFailed,
}

/// <summary>
/// Metadata of a binary stored in Garage (table <c>files</c>). It can be downloaded only when
/// <see cref="Status"/> is <see cref="FileStatus.Available"/> (BR-FILE-01). <see cref="ClassId"/> is kept
/// for every context except avatars, to compute the class quota (BR-FILE-02).
/// </summary>
public class StoredFile : IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
{
    public Guid Id { get; set; }

    public Guid InstitutionId { get; set; }

    public Guid? ClassId { get; set; }

    public FileContextType ContextType { get; set; }

    public Guid ContextId { get; set; }

    public Guid? ChatMessageId { get; set; }

    /// <summary>Only for display and <c>Content-Disposition</c>; never part of the object key.</summary>
    public required string OriginalName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    /// <summary><c>{institutionId}/{contextType}/{contextId}/{fileId}</c>.</summary>
    public required string ObjectKey { get; set; }

    public FileStatus Status { get; set; }

    public DateTimeOffset? ScannedAt { get; set; }

    public Guid UploadedById { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
