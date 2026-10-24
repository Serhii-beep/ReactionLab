using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.PlatformRoles;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class PlatformRoleChangeConfiguration : IEntityTypeConfiguration<PlatformRoleChange>
{
    public void Configure(EntityTypeBuilder<PlatformRoleChange> builder)
    {
        builder.ToTable("platform_role_changes");
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.Role).HasMaxLength(PlatformRoleChange.MaximumRoleLength).IsRequired();
        builder.Property(change => change.Source).HasMaxLength(PlatformRoleChange.MaximumSourceLength).IsRequired();
        builder.HasIndex(change => new { change.AccountId, change.Id });
    }
}
