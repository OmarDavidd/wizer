namespace Wizer.Api.Shared.Persistence;

/// <summary>Every table has <c>created_at</c>. The audit interceptor fills it from <c>IClock</c> when it is unset.</summary>
public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Editable entities have <c>updated_at</c>. The audit interceptor refreshes it on every update.</summary>
public interface IHasUpdatedAt
{
    DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Soft-deleted entities. Their configuration adds the <c>DeletedAt == null</c> query filter.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}
