using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Identity.Entities;

namespace Wizer.Api.Modules.Identity.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins", table => table.HasEnumCheck<ExternalLoginProvider>("provider"));
        builder.HasKey(login => login.Id);
        builder.Property(login => login.Provider).HasSnakeCaseConversion();
        builder.Property(login => login.ProviderKey).HasMaxLength(255);
        builder.Property(login => login.Email).HasMaxLength(320);

        builder.HasOne<User>().WithMany().HasForeignKey(login => login.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(login => new { login.Provider, login.ProviderKey }).IsUnique().HasDatabaseName("ux_external_logins_provider_provider_key");
        builder.HasIndex(login => new { login.UserId, login.Provider }).IsUnique().HasDatabaseName("ux_external_logins_user_id_provider");
    }
}
