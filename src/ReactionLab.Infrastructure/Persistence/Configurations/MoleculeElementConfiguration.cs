using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class MoleculeElementConfiguration : IEntityTypeConfiguration<MoleculeElement>
{
    public void Configure(EntityTypeBuilder<MoleculeElement> builder)
    {
        builder.ToTable("MoleculeElement");

        builder.HasKey(x => x.Id);

        builder.HasIndex(me => new { me.MoleculeId, me.ElementId })
            .IsUnique();

        builder.HasOne(me => me.Molecule)
            .WithMany(m => m.MoleculeElements)
            .HasForeignKey(me => me.MoleculeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(me => me.Element)
            .WithMany(e => e.MoleculeElements)
            .HasForeignKey(me => me.ElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}