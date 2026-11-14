using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Events;

internal sealed class LockoutNotices(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestLockoutWorthTelling = TimeSpan.FromDays(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var locked = consumed.Read(IdentityEventJsonContext.Default.AccountLocked);
        var now = timeProvider.GetUtcNow();
        var mailType = locked.Lock switch
        {
            AccountLocks.SignIn => MailTypes.SignInLocked,
            AccountLocks.PasswordChecks => MailTypes.PasswordChecksLocked,
            _ => null
        };

        if (mailType is null || consumed.OccurredAt < now - OldestLockoutWorthTelling)
        {
            return;
        }

        context.Add(MailJob.Queue(locked.UserId, consumed.Id, mailType, consumed.TraceParent, now));

        await context.SaveChangesAsync(cancellationToken);
    }
}
