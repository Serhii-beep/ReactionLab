using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class BondConfiguration : IEntityTypeConfiguration<Bond>
{
    public void Configure(EntityTypeBuilder<Bond> builder)
    {
        builder.ToTable("Bonds");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BondLength)
            .HasPrecision(6, 3);

        builder.Property(b => b.BondEnergy)
            .HasPrecision(8, 2);

        builder.HasOne(b => b.Molecule)
            .WithMany()
            .HasForeignKey(b => b.MoleculeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
