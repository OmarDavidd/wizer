using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Files.Entities;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Shared.Auth;

namespace Wizer.Api.Modules.Identity.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasEnumCheck<PlatformRole>("platform_role"));
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName).HasMaxLength(120);
        builder.Property(user => user.Email).HasMaxLength(320);
        builder.Property(user => user.PasswordHash).HasMaxLength(500);
        builder.Property(user => user.PlatformRole).HasSnakeCaseConversion();

        builder.HasOne<Institution>().WithMany().HasForeignKey(user => user.InstitutionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Campus>().WithMany().HasForeignKey(user => user.CampusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(user => user.AvatarFileId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("ux_users_email");
        builder.HasIndex(user => user.CampusId).HasDatabaseName("ix_users_campus_id");
    }
}
