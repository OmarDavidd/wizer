using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.Api.Shared.Ids;
using Wizer.Api.Shared.Observability;
using Wizer.Api.Shared.Time;

namespace Wizer.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// The first administrator of a deployment (.ia/database.md: "Producción solo siembra la institución y el
/// primer administrador, desde variables de entorno"). Runs at startup in any environment when
/// <c>Seed:AdminEmail</c> and <c>Seed:AdminPassword</c> are set, and does nothing once any administrator
/// exists, so the variables can stay or be removed after the first start.
/// </summary>
public sealed class AdminBootstrapper(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IIdGenerator ids,
    IClock clock,
    ILogger<AdminBootstrapper> logger)
{
    public const string EmailKey = "Seed:AdminEmail";
    public const string PasswordKey = "Seed:AdminPassword";
    public const string NameKey = "Seed:AdminName";
    public const string InstitutionNameKey = "Seed:InstitutionName";

    public async Task RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var email = configuration[EmailKey]?.Trim().ToLowerInvariant();
        var password = configuration[PasswordKey];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return;
        }

        if (await db.Set<User>().AnyAsync(user => user.PlatformRole == PlatformRole.Admin, cancellationToken))
        {
            logger.LogInformation("First administrator seed skipped: an administrator already exists");
            return;
        }

        if (password.Length < 10 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit) ||
            string.Equals(password.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{PasswordKey}' must have at least 10 characters, a letter and a number, and must not be the email.");
        }

        if (await db.Set<User>().AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new InvalidOperationException(
                $"'{EmailKey}' belongs to an existing account that is not an administrator; it is never promoted.");
        }

        var institution = await FindOrCreateInstitutionAsync(configuration[InstitutionNameKey], cancellationToken);
        var admin = new User
        {
            Id = ids.NewId(),
            InstitutionId = institution.Id,
            FullName = string.IsNullOrWhiteSpace(configuration[NameKey]) ? "Administración" : configuration[NameKey]!.Trim(),
            Email = email,
            PlatformRole = PlatformRole.Admin,
            IsActive = true,
            EmailVerifiedAt = clock.UtcNow,
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);
        db.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("First administrator {AdminEmail} created in {InstitutionName}",
            LogMasking.Email(email), institution.Name);
    }

    private async Task<Institution> FindOrCreateInstitutionAsync(string? name, CancellationToken cancellationToken)
    {
        var institutions = await db.Set<Institution>().Take(2).ToListAsync(cancellationToken);
        switch (institutions.Count)
        {
            case 1:
                return institutions[0];
            case > 1:
                throw new InvalidOperationException("There is more than one institution; the MVP supports one.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"There is no institution yet: set '{InstitutionNameKey}' to create it.");
        }

        var institution = new Institution { Id = ids.NewId(), Name = name.Trim() };
        db.Add(institution);
        return institution;
    }
}
