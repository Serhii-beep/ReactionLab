using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Converters;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Configurations;

internal static class SubstanceMapping
{
    public static void MapContent(EntityTypeBuilder<Substance> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Formula).IsRequired();
        builder.Property(s => s.Kind).IsRequired();
        builder.Property(s => s.StateAtRoomTemperature).IsRequired();
        builder.Property(s => s.IsOrganic).IsRequired();
        builder.Property(s => s.Category).HasMaxLength(Substance.MaximumCategoryLength);
        builder.Property(s => s.RetiredAt);

        builder.Property(s => s.Structure)
            .HasColumnType("jsonb")
            .HasConversion<MolecularStructureConverter>();

        builder.Property(s => s.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("translations")
            .HasColumnType("jsonb")
            .HasConversion<SubstanceTranslationsConverter>()
            .IsRequired();

        builder.Ignore(s => s.Locales);
        builder.Ignore(s => s.DomainEvents);
    }
}
