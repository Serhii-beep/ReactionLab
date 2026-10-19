using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OpenIddictAuthorizationConfiguration
    : IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreAuthorization<Guid>>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreAuthorization<Guid>> builder)
    {
        builder.ToTable("openiddict_authorizations");

        builder.HasOne(authorization => authorization.Application)
            .WithMany(application => application.Authorizations)
            .HasConstraintName("fk_openiddict_authorizations_application_id");

        builder.HasIndex(authorization => authorization.CreationDate, "CreationDate");

        builder.HasIndex(authorization => authorization.CreationDate, "Revoked")
            .HasDatabaseName("ix_openiddict_authorizations_revoked")
            .HasFilter("status <> 'valid'");
    }
}
