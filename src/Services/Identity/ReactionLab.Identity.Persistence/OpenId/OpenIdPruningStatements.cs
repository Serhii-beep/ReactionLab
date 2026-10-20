using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.OpenId;

public static class OpenIdPruningStatements
{
    public const int TokenBatchSize = 1_000;
    public const int SessionBatchSize = 200;

    private const long PruningLockKey = 0x6964656E_70727565;

    public static Task<bool> TryLockPruningAsync(this AppDbContext context, CancellationToken cancellationToken) =>
        context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({PruningLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);

    public static Task<int> DeleteRedeemedTokensAsync(
        this AppDbContext context,
        DateTimeOffset redeemedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM openiddict_tokens
            WHERE id IN (
                SELECT id FROM openiddict_tokens
                WHERE status = 'redeemed' AND redemption_date < {redeemedBefore}
                ORDER BY redemption_date
                LIMIT {TokenBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    public static Task<int> DeleteUnusedCodesAndRequestsAsync(
        this AppDbContext context,
        DateTimeOffset expiredBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM openiddict_tokens
            WHERE id IN (
                SELECT id FROM openiddict_tokens
                WHERE status = 'valid' AND type <> 'urn:ietf:params:oauth:token-type:refresh_token'
                    AND expiration_date < {expiredBefore}
                ORDER BY expiration_date
                LIMIT {TokenBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    public static Task<int> DeleteRevokedSessionsAsync(this AppDbContext context, CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH ended AS (
                SELECT id FROM openiddict_authorizations
                WHERE status <> 'valid'
                ORDER BY creation_date
                LIMIT {SessionBatchSize}
                FOR UPDATE SKIP LOCKED),
            ended_tokens AS (
                DELETE FROM openiddict_tokens WHERE authorization_id IN (SELECT id FROM ended))
            DELETE FROM openiddict_authorizations WHERE id IN (SELECT id FROM ended)
            """,
            cancellationToken);

    public static Task<int> DeleteExpiredSessionsAsync(
        this AppDbContext context,
        DateTimeOffset createdBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH ended AS (
                SELECT id FROM openiddict_authorizations
                WHERE creation_date < {createdBefore}
                ORDER BY creation_date
                LIMIT {SessionBatchSize}
                FOR UPDATE SKIP LOCKED),
            ended_tokens AS (
                DELETE FROM openiddict_tokens WHERE authorization_id IN (SELECT id FROM ended))
            DELETE FROM openiddict_authorizations WHERE id IN (SELECT id FROM ended)
            """,
            cancellationToken);
}
