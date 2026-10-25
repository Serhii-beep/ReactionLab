using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OrganizationChangeConfiguration : IEntityTypeConfiguration<OrganizationChange>
{
    public void Configure(EntityTypeBuilder<OrganizationChange> builder)
    {
        builder.ToTable("organization_changes");
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.Change).HasMaxLength(OrganizationChange.MaximumChangeLength).IsRequired();
        builder.Property(change => change.Role).HasMaxLength(OrganizationChange.MaximumRoleLength);
        builder.HasIndex(change => new { change.OrganizationId, change.Id });
    }
}
