using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Events;

internal sealed class AccountSetupRequests(AppDbContext context, TimeProvider timeProvider)
{
    private static readonly TimeSpan OldestSetupWorthSending = TimeSpan.FromDays(1);
    private static readonly TimeSpan OldestSelfResetWorthSending = TimeSpan.FromHours(1);

    public async Task QueueAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var requested = consumed.Read(IdentityEventJsonContext.Default.AccountSetupRequested);
        var now = timeProvider.GetUtcNow();
        var (mailType, oldestWorthSending) = requested.Purpose switch
        {
            AccountSetupPurposes.Invitation => (MailTypes.SchoolInvitation, OldestSetupWorthSending),
            AccountSetupPurposes.AdminReset => (MailTypes.SchoolPasswordReset, OldestSetupWorthSending),
            AccountSetupPurposes.SelfReset => (MailTypes.SchoolPasswordReset, OldestSelfResetWorthSending),
            _ => ((string?)null, TimeSpan.Zero)
        };

        if (mailType is null || consumed.OccurredAt < now - oldestWorthSending)
        {
            return;
        }

        context.Add(MailJob.Queue(requested.UserId, requested.RequestId, mailType, consumed.TraceParent, now));

        await context.SaveChangesAsync(cancellationToken);
    }
}
