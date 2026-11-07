using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging.Consuming;

public sealed class ErasureMarkerConfiguration : IEntityTypeConfiguration<ErasureMarker>
{
    public void Configure(EntityTypeBuilder<ErasureMarker> builder)
    {
        builder.ToTable("erasure_markers");
        builder.HasKey(marker => marker.Subject).HasName("pk_erasure_markers");

        builder.Property(marker => marker.Subject)
            .HasColumnName("subject")
            .HasMaxLength(OutboxMessage.MaximumSubjectLength);

        builder.Property(marker => marker.Version).HasColumnName("version");
        builder.Property(marker => marker.ErasedAt).HasColumnName("erased_at");

        builder.HasIndex(marker => marker.ErasedAt).HasDatabaseName("ix_erasure_markers_erased_at");
    }
}
