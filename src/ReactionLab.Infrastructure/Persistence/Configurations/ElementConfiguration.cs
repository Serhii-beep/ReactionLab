using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class ElementConfiguration : IEntityTypeConfiguration<Element>
{
    public void Configure(EntityTypeBuilder<Element> builder)
    {
        builder.ToTable("Elements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AtomicNumber)
            .IsRequired();

        builder.HasIndex(e => e.AtomicNumber)
            .IsUnique();

        builder.Property(e => e.Symbol)
            .IsRequired()
            .HasMaxLength(3);

        builder.HasIndex(e => e.Symbol)
            .IsUnique();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AtomicMass)
            .HasPrecision(10, 6);

        builder.Property(e => e.Electronegativity)
            .HasPrecision(4, 2);

        builder.Property(e => e.AtomicRadius)
            .HasPrecision(6, 2);

        builder.Property(e => e.IonizationEnergy)
            .HasPrecision(8, 2);

        builder.Property(e => e.MeltingPoint)
            .HasPrecision(8, 2);

        builder.Property(e => e.BoilingPoint)
            .HasPrecision(8, 2);

        builder.Property(e => e.Density)
            .HasPrecision(10, 4);

        builder.Property(e => e.Color)
            .HasMaxLength(50);

        builder.Property(e => e.ElectronConfiguration)
            .HasMaxLength(100);

        builder.Property(e => e.DisplayColor)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(e => e.Radius3D)
            .HasPrecision(4, 2);
    }
}