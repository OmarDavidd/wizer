namespace Wizer.Api.Infrastructure.Email;

/// <summary>A plain-text email. Bodies can hold codes or links: never log them.</summary>
public sealed record EmailMessage(string To, string Subject, string TextBody);
