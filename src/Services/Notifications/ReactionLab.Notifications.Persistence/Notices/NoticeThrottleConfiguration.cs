using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Persistence.Notices;

internal sealed class NoticeThrottleConfiguration : IEntityTypeConfiguration<NoticeThrottle>
{
    public void Configure(EntityTypeBuilder<NoticeThrottle> builder)
    {
        builder.ToTable("notice_throttles");
        builder.HasKey(throttle => new { throttle.RecipientId, throttle.MailType });

        builder.Property(throttle => throttle.MailType).HasMaxLength(MailJob.MaximumMailTypeLength);

        builder.HasIndex(throttle => throttle.LastQueuedAt);
    }
}
