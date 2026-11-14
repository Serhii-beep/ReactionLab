using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Events;

internal sealed class PasswordChangeNotices(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestChangeWorthTelling = TimeSpan.FromDays(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var changed = consumed.Read(IdentityEventJsonContext.Default.PasswordChanged);
        var now = timeProvider.GetUtcNow();

        if (consumed.OccurredAt < now - OldestChangeWorthTelling)
        {
            return;
        }

        context.Add(MailJob.Queue(
            changed.UserId,
            changed.ChangeId,
            MailTypes.PasswordChanged,
            consumed.TraceParent,
            now));

        await context.SaveChangesAsync(cancellationToken);
    }
}
