using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Erasures;

public static class AccountErasureStatements
{
    public const int EscalationBatchSize = 1_000;
    public const int PruningBatchSize = 1_000;

    public static Task<AccountErasure?> LockAccountErasureAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Set<AccountErasure>()
            .FromSql($"SELECT * FROM account_erasures WHERE user_id = {accountId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    public static Task<int> ConfirmAccountErasureAsync(
        this AppDbContext context,
        Guid accountId,
        string service,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken) =>
        context.Set<ErasureConfirmation>()
            .Where(confirmation => confirmation.UserId == accountId
                && confirmation.Service == service
                && confirmation.ConfirmedAt == null)
            .ExecuteUpdateAsync(
                confirmation => confirmation.SetProperty(row => row.ConfirmedAt, confirmedAt),
                cancellationToken);

    public static Task<int> CompleteAccountErasureAsync(
        this AppDbContext context,
        Guid accountId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken) =>
        context.Set<AccountErasure>()
            .Where(erasure => erasure.UserId == accountId
                && erasure.CompletedAt == null
                && !context.Set<ErasureConfirmation>().Any(confirmation =>
                    confirmation.UserId == accountId && confirmation.ConfirmedAt == null))
            .ExecuteUpdateAsync(
                erasure => erasure.SetProperty(row => row.CompletedAt, completedAt),
                cancellationToken);

    public static Task<List<EscalatedErasure>> EscalateOverdueErasuresAsync(
        this AppDbContext context,
        DateTimeOffset overdueBefore,
        DateTimeOffset escalatedAt,
        CancellationToken cancellationToken) =>
        context.Database.SqlQuery<EscalatedErasure>(
            $"""
            WITH overdue AS (
                SELECT user_id FROM account_erasures
                WHERE completed_at IS NULL AND requested_at < {overdueBefore}
                    AND (escalated_at IS NULL OR escalated_at < {overdueBefore})
                ORDER BY requested_at
                LIMIT {EscalationBatchSize}
                FOR UPDATE SKIP LOCKED)
            UPDATE account_erasures AS erasure SET escalated_at = {escalatedAt}
            FROM overdue
            WHERE erasure.user_id = overdue.user_id
            RETURNING erasure.user_id, erasure.version, erasure.organization_id, erasure.requested_at, (
                SELECT string_agg(pending.service, ', ' ORDER BY pending.service)
                FROM erasure_confirmations AS pending
                WHERE pending.user_id = erasure.user_id AND pending.confirmed_at IS NULL) AS pending_services
            """)
            .ToListAsync(cancellationToken);

    public static Task<int> CountOverdueErasuresAsync(
        this AppDbContext context,
        DateTimeOffset requestedBefore,
        CancellationToken cancellationToken) =>
        context.Set<AccountErasure>().CountAsync(
            erasure => erasure.CompletedAt == null && erasure.RequestedAt < requestedBefore,
            cancellationToken);

    public static Task<int> DeleteExpiredErasuresAsync(
        this AppDbContext context,
        DateTimeOffset completedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM account_erasures
            WHERE user_id IN (
                SELECT user_id FROM account_erasures
                WHERE completed_at < {completedBefore}
                ORDER BY completed_at
                LIMIT {PruningBatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
