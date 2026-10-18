using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");
        builder.HasKey(r => r.Id);

        ReactionMapping.MapContent(builder);

        builder.HasIndex(r => r.Type);

        builder.Property<string>(PersistenceColumns.NaturalKey)
            .HasMaxLength(NaturalKeys.MaximumLength)
            .IsRequired();
        builder.HasIndex(PersistenceColumns.NaturalKey).IsUnique();

        builder.Property<Guid[]>(PersistenceColumns.ReactantSignature).IsRequired();
        builder.HasIndex(PersistenceColumns.ReactantSignature).HasMethod("gin");

        builder.OwnsMany(r => r.Participants, p =>
        {
            p.ToTable("reaction_participants");
            p.WithOwner().HasForeignKey("ReactionId");
            p.HasKey(p => p.Id);
            ReactionMapping.MapParticipantContent(p);
            p.HasIndex(x => x.SubstanceId);
            p.HasOne<Substance>()
                .WithMany()
                .HasForeignKey(x => x.SubstanceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.HasSearchText();
        builder.HasAuditTimestamps();
    }
}
