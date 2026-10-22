namespace Wizer.Api.Shared.Ids;

/// <summary>Primary keys are UUID v7 (ADR-07): time ordered, so inserts stay index friendly.</summary>
public interface IIdGenerator
{
    Guid NewId();
}

public sealed class UuidV7IdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.CreateVersion7();
}
