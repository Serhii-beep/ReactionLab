using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class LockAnnouncements
{
    public static LockAnnouncement For(OutboxWriter outbox, User account, string accountLock) =>
        new(
            outbox.CreateMessage(new AccountLocked(account.Id, account.Version, accountLock, Disabled: false)),
            outbox.CreateMessage(new AccountLocked(account.Id, account.Version, accountLock, Disabled: true)));
}
