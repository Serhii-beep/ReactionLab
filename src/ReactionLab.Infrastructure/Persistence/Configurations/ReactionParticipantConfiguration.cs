using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public class ReactionParticipantConfiguration : IEntityTypeConfiguration<ReactionParticipant>
{
    public void Configure(EntityTypeBuilder<ReactionParticipant> builder)
    {
        builder.ToTable("ReactionParticipants");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Coefficient)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne(rp => rp.Reaction)
            .WithMany(r => r.Participants)
            .HasForeignKey(rp => rp.ReactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Molecule)
            .WithMany(m => m.ReactionParticipants)
            .HasForeignKey(rp => rp.MoleculeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rp => rp.Element)
            .WithMany()
            .HasForeignKey(rp => rp.ElementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rp => new { rp.MoleculeId, rp.Role })
            .HasDatabaseName("IX_ReactionParticipants_MoleculeId_Role")
            .HasFilter("\"Role\" = 0");

        builder.HasIndex(rp => new { rp.ElementId, rp.Role })
            .HasDatabaseName("IX_ReactionParticipants_ElementId_Role")
            .HasFilter("\"Role\" = 0");

        builder.HasIndex(rp => rp.ReactionId)
            .HasDatabaseName("IX_ReactionParticipants_ReactionId");
    }
}
