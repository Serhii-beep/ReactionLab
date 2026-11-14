using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.PlatformRoles;

public static class PlatformAdminAnnouncements
{
    public static async Task AnnouncePlatformAdminChangeAsync(
        this AppDbContext context,
        OutboxWriter outbox,
        Guid changeId,
        Guid changedAccountId)
    {
        var recipients = await context.ListPlatformAdminRecipientsAsync(changedAccountId, CancellationToken.None);

        await outbox.InsertAllAsync(
            context,
            recipients.Select(recipient =>
                new PlatformAdminChangeAnnounced(recipient.AccountId, recipient.Version, changeId)));
    }
}
