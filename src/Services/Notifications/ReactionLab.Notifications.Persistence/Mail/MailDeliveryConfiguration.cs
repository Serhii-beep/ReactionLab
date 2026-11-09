using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Notifications.Persistence.Mail;

internal sealed class MailDeliveryConfiguration : IEntityTypeConfiguration<MailDelivery>
{
    public void Configure(EntityTypeBuilder<MailDelivery> builder)
    {
        builder.ToTable("mail_deliveries");
        builder.HasKey(delivery => new { delivery.RecipientId, delivery.Id });

        builder.Property(delivery => delivery.Id).ValueGeneratedNever();
        builder.Property(delivery => delivery.MailType).HasMaxLength(MailJob.MaximumMailTypeLength);

        builder.HasIndex(delivery => delivery.SentAt);
    }
}
