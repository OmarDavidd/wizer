using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Modules.Campuses.Entities;

namespace Wizer.Api.Modules.Campuses.Configurations;

public sealed class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institutions");
        builder.HasKey(institution => institution.Id);
        builder.Property(institution => institution.Name).HasMaxLength(200);
    }
}
