using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Classes.Configurations;

public sealed class ClassMembershipConfiguration : IEntityTypeConfiguration<ClassMembership>
{
    public void Configure(EntityTypeBuilder<ClassMembership> builder)
    {
        builder.ToTable("class_memberships", table => table.HasEnumCheck<ClassRole>("role"));
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Role).HasSnakeCaseConversion();

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(membership => membership.ClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(membership => membership.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => new { membership.ClassId, membership.UserId })
            .IsUnique()
            .HasDatabaseName("ux_class_memberships_class_id_user_id");
        // RN-02: a single teacher per class.
        builder.HasIndex(membership => membership.ClassId)
            .IsUnique()
            .HasFilter("role = 'teacher'")
            .HasDatabaseName("ux_class_memberships_class_id_teacher");
        builder.HasIndex(membership => membership.UserId).HasDatabaseName("ix_class_memberships_user_id");
    }
}
