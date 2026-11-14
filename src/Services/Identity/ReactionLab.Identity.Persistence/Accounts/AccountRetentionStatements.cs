using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class AccountRetentionStatements
{
    public const int ErasureBatchSize = 100;

    public static Task<List<ErasedAccount>> EraseUnconfirmedAccountsAsync(
        this AppDbContext context,
        TimeSpan confirmationPeriod) =>
        context.Database.SqlQuery<ErasedAccount>(
            $"""
            WITH expired AS MATERIALIZED (
                SELECT users.id FROM users
                WHERE users.organization_id IS NULL AND users.email IS NOT NULL
                    AND NOT users.email_confirmed AND NOT users.email_confirmation_exempt
                    AND users.created_at < now() - {confirmationPeriod}
                    AND NOT EXISTS (SELECT 1 FROM user_platform_roles WHERE user_platform_roles.user_id = users.id)
                    AND NOT EXISTS (SELECT 1 FROM user_school_roles WHERE user_school_roles.user_id = users.id)
                ORDER BY users.created_at
                LIMIT {ErasureBatchSize}
                FOR UPDATE SKIP LOCKED),
            ended AS (
                UPDATE openiddict_authorizations SET status = 'revoked'
                WHERE subject = ANY (ARRAY(SELECT id::text FROM expired)) AND status = 'valid'
                RETURNING id),
            ended_tokens AS (
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid')
            DELETE FROM users
            WHERE id IN (SELECT id FROM expired)
            RETURNING id, version
            """)
            .ToListAsync(CancellationToken.None);
}
