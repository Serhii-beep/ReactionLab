using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Notifications.Persistence.Mail;

public static class MailDeliveryQueries
{
    public static Task<bool> WasMailDeliveredSinceAsync(
        this AppDbContext context,
        Guid recipientId,
        string mailType,
        DateTimeOffset since,
        CancellationToken cancellationToken) =>
        context.Set<MailDelivery>().AnyAsync(
            delivery => delivery.RecipientId == recipientId && delivery.MailType == mailType && delivery.SentAt >= since,
            cancellationToken);
}
