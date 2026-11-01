using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", table => table.HasCheckConstraint(
            "ck_outbox_messages_tombstone",
            "(type IS NULL) = (payload IS NULL)"));
        builder.HasKey(message => message.Id).HasName("pk_outbox_messages");

        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(message => message.Position)
            .HasColumnName("position")
            .UseIdentityAlwaysColumn()
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(message => new { message.Topic, message.Position })
            .HasDatabaseName("ix_outbox_messages_topic_position")
            .IsUnique();

        builder.HasIndex(message => message.Subject).HasDatabaseName("ix_outbox_messages_subject");

        builder.Property(message => message.Topic)
            .HasColumnName("topic")
            .HasMaxLength(OutboxMessage.MaximumTopicLength);

        builder.Property(message => message.Subject)
            .HasColumnName("subject")
            .HasMaxLength(OutboxMessage.MaximumSubjectLength);

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(OutboxMessage.MaximumTypeLength);

        builder.Property(message => message.Payload).HasColumnName("payload");

        builder.Property(message => message.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(OutboxMessage.TraceParentLength);

        builder.Property(message => message.CreatedAt).HasColumnName("created_at");
    }
}
