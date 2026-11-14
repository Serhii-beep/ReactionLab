using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.AccountSetups;

internal sealed class MailedAccountSetups(AppDbContext context, OutboxWriter outbox)
{
    public async Task<bool> RequestAsync(Guid organizationId, Guid accountId, Guid requestedBy, Guid requestedInSession)
    {
        var target = await context.FindMailedSetupTargetAsync(accountId);
        var requested = new AccountSetupRequested(accountId, target.Version, Guid.CreateVersion7(), target.Purpose);

        return await context.RequestMailedAccountSetupAsync(
            organizationId,
            accountId,
            requested.RequestId,
            requested.Purpose,
            requestedBy,
            requestedInSession,
            outbox.CreateMessage(requested)) == 1;
    }
}
