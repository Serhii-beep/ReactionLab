using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Maintenance;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class MaintenanceRunConfiguration : IEntityTypeConfiguration<MaintenanceRun>
{
    public void Configure(EntityTypeBuilder<MaintenanceRun> builder)
    {
        builder.ToTable("maintenance_runs");
        builder.HasKey(run => run.Name);
        builder.Property(run => run.Name).HasMaxLength(MaintenanceRun.MaximumNameLength);
    }
}
