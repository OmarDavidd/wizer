using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Submissions.Entities;

namespace Wizer.Api.Modules.Submissions.Configurations;

public sealed class SubmissionLinkConfiguration : IEntityTypeConfiguration<SubmissionLink>
{
    public void Configure(EntityTypeBuilder<SubmissionLink> builder)
    {
        builder.ToTable("submission_links");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Url).HasMaxLength(SubmissionLink.MaxUrlLength);
        builder.Property(link => link.Title).HasMaxLength(200);

        builder.HasOne<Submission>().WithMany().HasForeignKey(link => link.SubmissionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => link.SubmissionId).HasDatabaseName("ix_submission_links_submission_id");
    }
}
