using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Campuses.Entities;
using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Modules.Classes.Entities;
using Wizer.Api.Modules.Files.Entities;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Files.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("files", table =>
        {
            table.HasEnumCheck<FileContextType>("context_type");
            table.HasEnumCheck<FileStatus>("status");
            table.HasCheckConstraint("ck_files_size_bytes", "size_bytes > 0");
            table.HasCheckConstraint(
                "ck_files_class_id",
                "(context_type = 'avatar' AND class_id IS NULL) OR (context_type <> 'avatar' AND class_id IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_files_chat_message_id",
                "chat_message_id IS NULL OR context_type = 'chat_attachment'");
        });
        builder.HasKey(file => file.Id);
        builder.HasQueryFilter(file => file.DeletedAt == null);

        builder.Property(file => file.ContextType).HasSnakeCaseConversion();
        builder.Property(file => file.Status).HasSnakeCaseConversion();
        builder.Property(file => file.OriginalName).HasMaxLength(255);
        builder.Property(file => file.ContentType).HasMaxLength(150);
        builder.Property(file => file.ObjectKey).HasMaxLength(500);

        builder.HasOne<Institution>().WithMany().HasForeignKey(file => file.InstitutionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(file => file.ClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChatMessage>().WithMany().HasForeignKey(file => file.ChatMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(file => file.UploadedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(file => file.ObjectKey).IsUnique().HasDatabaseName("ux_files_object_key");
        // Class quota (BR-FILE-02).
        builder.HasIndex(file => file.ClassId).HasFilter("deleted_at IS NULL").HasDatabaseName("ix_files_class_id");
        builder.HasIndex(file => new { file.ContextType, file.ContextId }).HasDatabaseName("ix_files_context_type_context_id");
        builder.HasIndex(file => file.ChatMessageId).HasDatabaseName("ix_files_chat_message_id");
        // Workers: stale uploads cleanup and scan re-queue on startup.
        builder.HasIndex(file => file.Status)
            .HasFilter("status IN ('uploading', 'pending_scan')")
            .HasDatabaseName("ix_files_status_pending");
    }
}
