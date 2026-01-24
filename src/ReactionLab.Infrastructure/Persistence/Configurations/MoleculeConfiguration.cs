using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class MoleculeConfiguration : IEntityTypeConfiguration<Molecule>
{
    public void Configure(EntityTypeBuilder<Molecule> builder)
    {
        builder.ToTable("Molecules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Formula)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.IUPACName)
            .HasMaxLength(300);

        builder.Property(m => m.MolecularWeight)
            .HasPrecision(12, 4);

        builder.Property(m => m.Category)
            .HasMaxLength(100);

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(500);

        builder.Property(m => m.Model3DUrl)
            .HasMaxLength(500);
    }
}