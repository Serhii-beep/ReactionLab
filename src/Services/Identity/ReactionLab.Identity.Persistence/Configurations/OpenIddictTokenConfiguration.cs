using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OpenIddictTokenConfiguration
    : IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreToken<Guid>>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreToken<Guid>> builder)
    {
        builder.ToTable("openiddict_tokens");

        builder.HasOne(token => token.Application)
            .WithMany(application => application.Tokens)
            .HasConstraintName("fk_openiddict_tokens_application_id");

        builder.HasOne(token => token.Authorization)
            .WithMany(authorization => authorization.Tokens)
            .HasConstraintName("fk_openiddict_tokens_authorization_id");

        builder.HasIndex(token => token.ReferenceId)
            .HasFilter("reference_id IS NOT NULL");

        builder.HasIndex(token => token.RedemptionDate)
            .HasDatabaseName("ix_openiddict_tokens_redeemed")
            .HasFilter("status = 'redeemed'");

        builder.HasIndex(token => token.ExpirationDate)
            .HasDatabaseName("ix_openiddict_tokens_unused")
            .HasFilter("status = 'valid' AND type <> 'urn:ietf:params:oauth:token-type:refresh_token'");
    }
}
