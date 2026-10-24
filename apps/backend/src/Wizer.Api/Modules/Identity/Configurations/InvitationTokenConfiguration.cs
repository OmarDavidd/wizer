using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Configurations;

public sealed class InvitationTokenConfiguration : IEntityTypeConfiguration<InvitationToken>
{
    public void Configure(EntityTypeBuilder<InvitationToken> builder)
    {
        builder.ToTable("invitation_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(64);

        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.InvitedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_invitation_tokens_token_hash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_invitation_tokens_user_id");
    }
}
