using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal sealed class PasswordChanges(AppDbContext context, OutboxWriter outbox)
{
    public async Task RecordAsync(Guid accountId, Guid changeId, string? userAgent)
    {
        if (await context.RecordPasswordChangeAsync(changeId, accountId, Bounded(userAgent)) == 0)
        {
            return;
        }

        var version = await context.Users
            .Where(user => user.Id == accountId)
            .Select(user => user.Version)
            .SingleAsync(CancellationToken.None);

        await outbox.InsertAsync(context, new PasswordChanged(accountId, version, changeId));
    }

    private static string? Bounded(string? userAgent) =>
        string.IsNullOrEmpty(userAgent)
            ? null
            : userAgent.Length > PasswordChange.MaximumUserAgentLength
                ? userAgent[..PasswordChange.MaximumUserAgentLength]
                : userAgent;
}
