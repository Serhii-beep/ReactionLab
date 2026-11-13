using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Events;

internal sealed class EmailConfirmationRequests(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestRequestWorthSending = TimeSpan.FromDays(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var requested = consumed.Read(IdentityEventJsonContext.Default.EmailConfirmationRequested);
        var now = timeProvider.GetUtcNow();

        if (consumed.OccurredAt < now - OldestRequestWorthSending)
        {
            return;
        }

        context.Add(MailJob.Queue(
            requested.UserId,
            requested.RequestId,
            MailTypes.EmailConfirmation,
            consumed.TraceParent,
            now));

        await context.SaveChangesAsync(cancellationToken);
    }
}
