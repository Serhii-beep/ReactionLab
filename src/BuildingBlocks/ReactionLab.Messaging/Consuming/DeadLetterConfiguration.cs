using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging.Consuming;

public sealed class DeadLetterConfiguration : IEntityTypeConfiguration<DeadLetter>
{
    public void Configure(EntityTypeBuilder<DeadLetter> builder)
    {
        builder.ToTable("dead_letters");
        builder.HasKey(letter => letter.Id).HasName("pk_dead_letters");

        builder.Property(letter => letter.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(letter => letter.Topic)
            .HasColumnName("topic")
            .HasMaxLength(OutboxMessage.MaximumTopicLength);

        builder.Property(letter => letter.Partition).HasColumnName("partition");
        builder.Property(letter => letter.Offset).HasColumnName("offset");
        builder.Property(letter => letter.EventId).HasColumnName("event_id");

        builder.Property(letter => letter.Type)
            .HasColumnName("type")
            .HasMaxLength(OutboxMessage.MaximumTypeLength);

        builder.Property(letter => letter.Subject)
            .HasColumnName("subject")
            .HasMaxLength(OutboxMessage.MaximumSubjectLength);

        builder.Property(letter => letter.OccurredAt).HasColumnName("occurred_at");
        builder.Property(letter => letter.Payload).HasColumnName("payload");

        builder.Property(letter => letter.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(OutboxMessage.TraceParentLength);

        builder.Property(letter => letter.Failure).HasColumnName("failure");
        builder.Property(letter => letter.FailedAt).HasColumnName("failed_at");

        builder.HasIndex(letter => letter.Subject).HasDatabaseName("ix_dead_letters_subject");
        builder.HasIndex(letter => letter.FailedAt).HasDatabaseName("ix_dead_letters_failed_at");
    }
}
