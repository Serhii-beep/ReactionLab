using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Persistence.Notices;

namespace ReactionLab.Notifications.Events;

internal sealed class RepeatedRegistrationNotices(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestEventWorthSending = TimeSpan.FromDays(1);
    private static readonly TimeSpan QuietPeriodAfterRegistration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IntervalBetweenNotices = TimeSpan.FromDays(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var repeated = consumed.Read(IdentityEventJsonContext.Default.RegistrationRepeated);
        var now = timeProvider.GetUtcNow();

        if (consumed.OccurredAt < now - OldestEventWorthSending
            || consumed.OccurredAt < repeated.RegisteredAt + QuietPeriodAfterRegistration)
        {
            return;
        }

        var claimed = await context.TryClaimNoticeAsync(
            repeated.UserId,
            MailTypes.RepeatedRegistration,
            consumed.OccurredAt < now ? consumed.OccurredAt : now,
            IntervalBetweenNotices,
            cancellationToken);

        if (claimed)
        {
            context.Add(MailJob.Queue(
                repeated.UserId,
                consumed.Id,
                MailTypes.RepeatedRegistration,
                consumed.TraceParent,
                now));

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
