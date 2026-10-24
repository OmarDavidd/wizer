using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(64);

        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RefreshToken>().WithMany().HasForeignKey(token => token.ReplacedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_token_hash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        builder.HasIndex(token => token.FamilyId).HasDatabaseName("ix_refresh_tokens_family_id");
    }
}
