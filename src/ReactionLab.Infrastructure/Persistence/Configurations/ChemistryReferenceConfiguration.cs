using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Reference;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

internal sealed class ChemistryReferenceConfiguration : IEntityTypeConfiguration<ChemistryReference>
{
    public void Configure(EntityTypeBuilder<ChemistryReference> builder)
    {
        builder.ToTable("chemistry_reference");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Key).IsRequired();
        builder.HasIndex(r => r.Key).IsUnique();

        builder.Property(r => r.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasAuditTimestamps();

        builder.Ignore(r => r.DomainEvents);
    }
}
