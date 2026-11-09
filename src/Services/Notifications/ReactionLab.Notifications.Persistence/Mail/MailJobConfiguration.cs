using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Notifications.Persistence.Mail;

internal sealed class MailJobConfiguration : IEntityTypeConfiguration<MailJob>
{
    public void Configure(EntityTypeBuilder<MailJob> builder)
    {
        builder.ToTable("mail_jobs");
        builder.HasKey(job => new { job.RecipientId, job.Id });

        builder.Property(job => job.Id).ValueGeneratedNever();
        builder.Property(job => job.MailType).HasMaxLength(MailJob.MaximumMailTypeLength);
        builder.Property(job => job.TraceParent).HasMaxLength(OutboxMessage.TraceParentLength);
        builder.Property(job => job.LastFailure).HasMaxLength(MailJob.MaximumFailureLength);

        builder.HasIndex(job => job.AvailableAt)
            .HasDatabaseName("ix_mail_jobs_due")
            .HasFilter("failed_at IS NULL");

        builder.HasIndex(job => job.FailedAt)
            .HasDatabaseName("ix_mail_jobs_failed")
            .HasFilter("failed_at IS NOT NULL");
    }
}
