using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Converters;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

internal sealed class SubstanceConfiguration : IEntityTypeConfiguration<Substance>
{
    public void Configure(EntityTypeBuilder<Substance> builder)
    {
        builder.ToTable("substances");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Formula).IsRequired();
        builder.HasIndex(s => s.Formula);

        builder.Property(s => s.Kind).IsRequired();
        builder.Property(s => s.StateAtRoomTemperature).IsRequired();
        builder.Property(s => s.IsOrganic).IsRequired();

        builder.Property(s => s.Category).HasMaxLength(Substance.MaximumCategoryLength);
        builder.HasIndex(s => s.Category);

        builder.Property(s => s.Structure)
            .HasColumnType("jsonb")
            .HasConversion<MolecularStructureConverter>();

        builder.Property(s => s.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("translations")
            .HasColumnType("jsonb")
            .HasConversion<SubstanceTranslationsConverter>()
            .IsRequired();

        builder.HasSearchText();
        builder.HasAuditTimestamps();

        builder.Ignore(s => s.Locales);
        builder.Ignore(s => s.DomainEvents);
    }
}
