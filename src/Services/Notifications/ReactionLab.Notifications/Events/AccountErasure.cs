using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Persistence.Notices;

namespace ReactionLab.Notifications.Events;

internal sealed class AccountErasure(AppDbContext context, TimeProvider timeProvider)
{
    public async Task EraseAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var requested = consumed.Read(IdentityEventJsonContext.Default.UserErasureRequested);

        await context.Set<MailJob>()
            .Where(job => job.RecipientId == requested.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<NoticeThrottle>()
            .Where(throttle => throttle.RecipientId == requested.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.RecordErasureAsync(
            consumed.Subject,
            requested.Version,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
