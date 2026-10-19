using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OpenIddictApplicationConfiguration
    : IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreApplication<Guid>>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreApplication<Guid>> builder) =>
        builder.ToTable("openiddict_applications");
}
