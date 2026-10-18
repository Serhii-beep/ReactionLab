using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Chemistry.Constitution;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class SubstanceConfiguration : IEntityTypeConfiguration<Substance>
{
    public void Configure(EntityTypeBuilder<Substance> builder)
    {
        builder.ToTable("substances");
        builder.HasKey(s => s.Id);

        SubstanceMapping.MapContent(builder);

        builder.HasIndex(s => s.Formula);
        builder.HasIndex(s => s.Category);

        builder.Property<string[]>(PersistenceColumns.ElementSymbols).IsRequired();
        builder.HasIndex(PersistenceColumns.ElementSymbols).HasMethod("gin");

        builder.Property<string>(PersistenceColumns.NaturalKey)
            .HasMaxLength(NaturalKeys.MaximumLength)
            .IsRequired();
        builder.HasIndex(PersistenceColumns.NaturalKey).IsUnique();

        builder.Property<string?>(PersistenceColumns.StructureKey).HasMaxLength(ConstitutionKey.Length);
        builder.HasIndex(PersistenceColumns.StructureKey);

        builder.HasSearchText();
        builder.HasAuditTimestamps();
    }
}
