using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Events;

internal sealed class PasswordResetRequests(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestRequestWorthSending = TimeSpan.FromHours(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var requested = consumed.Read(IdentityEventJsonContext.Default.PasswordResetRequested);
        var now = timeProvider.GetUtcNow();

        if (consumed.OccurredAt < now - OldestRequestWorthSending)
        {
            return;
        }

        context.Add(MailJob.Queue(
            requested.UserId,
            requested.RequestId,
            MailTypes.PasswordReset,
            consumed.TraceParent,
            now));

        await context.SaveChangesAsync(cancellationToken);
    }
}
