using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Users.Entities;

namespace Wizer.Api.Modules.Users.Configurations;

public sealed class AcademicProfileConfiguration : IEntityTypeConfiguration<AcademicProfile>
{
    public void Configure(EntityTypeBuilder<AcademicProfile> builder)
    {
        builder.ToTable("academic_profiles", table =>
            table.HasCheckConstraint("ck_academic_profiles_semester", "semester BETWEEN 1 AND 20"));
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.UserId).ValueGeneratedNever();

        builder.Property(profile => profile.Career).HasMaxLength(150);
        builder.Property(profile => profile.EnrollmentNumber).HasMaxLength(30);
        builder.Property(profile => profile.ProfessionalTitle).HasMaxLength(150);

        builder.HasOne<User>().WithOne().HasForeignKey<AcademicProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
