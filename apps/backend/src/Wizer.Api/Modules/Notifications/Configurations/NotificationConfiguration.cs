using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;
using Wizer.Api.Modules.Notifications.Entities;

namespace Wizer.Api.Modules.Notifications.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table => table.HasEnumCheck<NotificationType>("type"));
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type).HasSnakeCaseConversion();
        builder.Property(notification => notification.Title).HasMaxLength(200);
        builder.Property(notification => notification.Body).HasMaxLength(500);
        builder.Property(notification => notification.Data).HasColumnType("jsonb");

        builder.HasOne<User>().WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_notifications_user_id_created_at");
        builder.HasIndex(notification => notification.UserId)
            .HasFilter("read_at IS NULL")
            .HasDatabaseName("ix_notifications_user_id_unread");
    }
}
