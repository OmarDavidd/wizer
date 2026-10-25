using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Modules.Identity.DTOs;

/// <summary>What the Admin module asks Identity to create: a teacher or administrator account.</summary>
public sealed record InviteUserCommand(string FullName, string Email, PlatformRole Role, Guid CampusId);
