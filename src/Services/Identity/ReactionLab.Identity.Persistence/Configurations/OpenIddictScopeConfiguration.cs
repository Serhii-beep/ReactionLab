using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OpenIddictScopeConfiguration
    : IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreScope<Guid>>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreScope<Guid>> builder) =>
        builder.ToTable("openiddict_scopes");
}
