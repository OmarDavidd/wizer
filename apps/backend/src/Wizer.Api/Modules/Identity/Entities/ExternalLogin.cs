using Wizer.Api.Shared.Persistence;

namespace Wizer.Api.Modules.Identity.Entities;

public enum ExternalLoginProvider
{
    Google,
}

/// <summary>Links a user to an external identity (the Google <c>sub</c>).</summary>
public class ExternalLogin : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ExternalLoginProvider Provider { get; set; }

    public required string ProviderKey { get; set; }

    /// <summary>The email the provider reported when the login was linked.</summary>
    public required string Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
