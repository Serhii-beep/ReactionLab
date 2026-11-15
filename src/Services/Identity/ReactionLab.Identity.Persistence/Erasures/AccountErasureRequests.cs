using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence.Erasures;

public static class AccountErasureRequests
{
    public static async Task RequestAccountErasureAsync(
        this AppDbContext context,
        OutboxWriter outbox,
        Guid accountId,
        long erasedVersion,
        Guid? organizationId,
        string reason,
        DateTimeOffset requestedAt)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("An erasure is requested inside the transaction that deletes it.");
        }

        string[] confirmingServices = [.. AccountErasure.ConfirmingServices];

        await context.Database.ExecuteSqlAsync(
            $"""
            WITH recorded AS (
                INSERT INTO account_erasures (user_id, version, organization_id, reason, requested_at)
                VALUES ({accountId}, {erasedVersion}, {organizationId}, {reason}, {requestedAt})
                ON CONFLICT (user_id) DO NOTHING
                RETURNING user_id)
            INSERT INTO erasure_confirmations (user_id, service)
            SELECT recorded.user_id, service FROM recorded, unnest({confirmingServices}) AS service
            """,
            CancellationToken.None);

        await outbox.InsertAsync(context, new UserErasureRequested(accountId, erasedVersion, organizationId));
        await outbox.InsertTombstoneAsync(context, IdentityTopics.Profiles, accountId);
    }
}
