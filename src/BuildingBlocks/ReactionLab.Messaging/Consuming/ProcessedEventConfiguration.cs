using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Messaging.Consuming;

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");
        builder.HasKey(processed => processed.EventId).HasName("pk_processed_events");

        builder.Property(processed => processed.EventId).HasColumnName("event_id").ValueGeneratedNever();
        builder.Property(processed => processed.ProcessedAt).HasColumnName("processed_at");

        builder.HasIndex(processed => processed.ProcessedAt).HasDatabaseName("ix_processed_events_processed_at");
    }
}
