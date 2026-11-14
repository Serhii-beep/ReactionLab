using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.Logins;

internal sealed class SignInMethodChanges(AppDbContext context, OutboxWriter outbox)
{
    public async Task RecordAsync(Guid accountId, Guid changeId, string change, string? provider, string? userAgent)
    {
        if (await context.RecordSignInMethodChangeAsync(
                changeId,
                accountId,
                change,
                provider,
                UserAgents.Bounded(userAgent)) == 0)
        {
            return;
        }

        var version = await context.Users
            .Where(user => user.Id == accountId)
            .Select(user => user.Version)
            .SingleAsync(CancellationToken.None);

        await outbox.InsertAsync(context, new SignInMethodChanged(accountId, version, changeId));
    }
}
