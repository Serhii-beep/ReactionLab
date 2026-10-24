using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.PlatformRoles;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    private const int MaximumNameLength = 256;

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("platform_roles");
        builder.Property(role => role.Name).HasMaxLength(MaximumNameLength);
        builder.Property(role => role.NormalizedName).HasMaxLength(MaximumNameLength);
        builder.HasIndex(role => role.NormalizedName).IsUnique();

        builder.HasData(
            SeededRole("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e01", PlatformRoleNames.Admin),
            SeededRole("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e02", PlatformRoleNames.Moderator),
            SeededRole("0199f0c8-6a35-7b52-9c1e-3f4a8d2b6e03", PlatformRoleNames.CatalogEditor));
    }

    private static IdentityRole<Guid> SeededRole(string id, string name) =>
        new()
        {
            Id = Guid.Parse(id),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = id
        };
}
