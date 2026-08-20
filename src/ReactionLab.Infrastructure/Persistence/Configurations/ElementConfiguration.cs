using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Elements;
using ReactionLab.Infrastructure.Persistence.Converters;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

internal sealed class ElementConfiguration : IEntityTypeConfiguration<Element>
{
    public void Configure(EntityTypeBuilder<Element> builder)
    {
        builder.ToTable("elements");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AtomicNumber).IsRequired();
        builder.HasIndex(e => e.AtomicNumber).IsUnique();

        builder.Property(e => e.Symbol).IsRequired().HasColumnType("citext");
        builder.HasIndex(e => e.Symbol).IsUnique();

        builder.Property(e => e.Mass).IsRequired();
        builder.Property(e => e.Category).IsRequired();
        builder.Property(e => e.StateAtRoomTemperature).IsRequired();
        builder.Property(e => e.DisplayColor).IsRequired();
        builder.Property(e => e.ElectronConfiguration).HasMaxLength(Element.MaximumElectronConfigurationLength);

        builder.ComplexProperty(e => e.Position, p =>
        {
            p.Property(x => x.Period).HasColumnName("period");
            p.Property(x => x.Group).HasColumnName("periodic_group");
        });

        builder.ComplexProperty(e => e.Radii, radii =>
        {
            radii.IsRequired(false);
            radii.Property(x => x.CovalentPicometers)
                .HasColumnName("covalent_radius_pm").HasPrecision(6, 2);
            radii.Property(x => x.VanDerWaalsPicometers)
                .HasColumnName("van_der_waals_radius_pm").HasPrecision(6, 2);
        });

        builder.Property(e => e.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("translations")
            .HasColumnType("jsonb")
            .HasConversion<ElementTranslationsConverter>()
            .IsRequired();

        builder.HasSearchText();
        builder.HasAuditTimestamps();

        builder.Ignore(e => e.Locales);
        builder.Ignore(e => e.DomainEvents);
    }
}
