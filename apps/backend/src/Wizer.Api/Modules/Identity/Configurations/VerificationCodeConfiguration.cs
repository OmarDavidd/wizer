using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Configurations;

public sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("verification_codes", table =>
        {
            table.HasEnumCheck<VerificationPurpose>("purpose");
            table.HasCheckConstraint("ck_verification_codes_attempts", "attempts >= 0");
        });
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Purpose).HasSnakeCaseConversion();
        builder.Property(code => code.CodeHash).HasMaxLength(64);

        builder.HasOne<User>().WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Cascade);

        // One active code per user and purpose: issuing a new one deletes the previous one first.
        builder.HasIndex(code => new { code.UserId, code.Purpose })
            .IsUnique()
            .HasFilter("consumed_at IS NULL")
            .HasDatabaseName("ux_verification_codes_user_id_purpose");
    }
}
