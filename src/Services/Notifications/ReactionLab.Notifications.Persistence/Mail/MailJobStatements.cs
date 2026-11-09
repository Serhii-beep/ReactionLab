using Microsoft.EntityFrameworkCore;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Notifications.Persistence.Mail;

public static class MailJobStatements
{
    public static Task<List<MailJob>> ClaimDueMailJobsAsync(
        this AppDbContext context,
        TimeSpan lease,
        int batchSize,
        CancellationToken cancellationToken) =>
        context.Set<MailJob>()
            .FromSql(
                $"""
                UPDATE mail_jobs
                SET available_at = now() + {lease}, attempts = attempts + 1
                WHERE (recipient_id, id) IN (
                    SELECT recipient_id, id FROM mail_jobs
                    WHERE available_at <= now() AND failed_at IS NULL
                    ORDER BY available_at
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED)
                RETURNING recipient_id, id, mail_type, trace_parent, available_at, created_at,
                    attempts, last_failure, failed_at
                """)
            .AsNoTracking()
        .ToListAsync(cancellationToken);

    public static Task<int> FinishSentMailJobAsync(
        this AppDbContext context,
        MailJob job,
        CancellationToken cancellationToken) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(
            token => context.Database.ExecuteSqlAsync(
                $"""
                WITH finished AS (
                    DELETE FROM mail_jobs
                    WHERE recipient_id = {job.RecipientId} AND id = {job.Id}
                    RETURNING recipient_id, id, mail_type)
                INSERT INTO mail_deliveries (recipient_id, id, mail_type, sent_at)
                SELECT recipient_id, id, mail_type, now() FROM finished
                ON CONFLICT (recipient_id, id) DO NOTHING
                """,
                token),
            cancellationToken);

    public static Task<int> DropMailJobAsync(
        this AppDbContext context,
        MailJob job,
        CancellationToken cancellationToken) =>
        context.Set<MailJob>()
            .Where(queued => queued.RecipientId == job.RecipientId && queued.Id == job.Id)
            .ExecuteDeleteAsync(cancellationToken);

    public static Task<int> RetryMailJobAsync(
        this AppDbContext context,
        MailJob job,
        TimeSpan delay,
        string failure,
        CancellationToken cancellationToken) =>
        context.Set<MailJob>()
            .Where(queued => queued.RecipientId == job.RecipientId && queued.Id == job.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(queued => queued.AvailableAt, queued => DateTimeOffset.UtcNow + delay)
                    .SetProperty(queued => queued.LastFailure, failure),
                cancellationToken);

    public static Task<int> DeferMailJobAsync(
        this AppDbContext context,
        MailJob job,
        TimeSpan delay,
        string failure,
        CancellationToken cancellationToken) =>
        context.Set<MailJob>()
            .Where(queued => queued.RecipientId == job.RecipientId && queued.Id == job.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(queued => queued.AvailableAt, queued => DateTimeOffset.UtcNow + delay)
                    .SetProperty(queued => queued.Attempts, queued => queued.Attempts - 1)
                    .SetProperty(queued => queued.LastFailure, failure),
                cancellationToken);

    public static Task<int> FailMailJobAsync(
        this AppDbContext context,
        MailJob job,
        string failure,
        CancellationToken cancellationToken) =>
        context.Set<MailJob>()
            .Where(queued => queued.RecipientId == job.RecipientId && queued.Id == job.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(queued => queued.FailedAt, queued => (DateTimeOffset?)DateTimeOffset.UtcNow)
                    .SetProperty(queued => queued.LastFailure, failure),
                cancellationToken);

    public static Task<int> ReleaseMailJobsAsync(
       this AppDbContext context,
       IReadOnlyCollection<MailJob> jobs,
       DateTimeOffset leasedUntil,
       CancellationToken cancellationToken)
    {
        var recipientIds = jobs.Select(job => job.RecipientId).ToArray();
        var ids = jobs.Select(job => job.Id).ToArray();

        return context.Database.CreateExecutionStrategy().ExecuteAsync(
            token => context.Database.ExecuteSqlAsync(
                $"""
                UPDATE mail_jobs AS job
                SET available_at = now(), attempts = job.attempts - 1
                FROM unnest({recipientIds}, {ids}) AS released (recipient_id, id)
                WHERE job.recipient_id = released.recipient_id
                    AND job.id = released.id
                    AND job.available_at = {leasedUntil}
                """,
                token),
            cancellationToken);
    }

    public static Task<int> DeleteFailedMailJobsAsync(
        this AppDbContext context,
        DateTimeOffset failedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM mail_jobs
            WHERE (recipient_id, id) IN (
                SELECT recipient_id, id FROM mail_jobs
                WHERE failed_at < {failedBefore}
                ORDER BY failed_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    public static Task<int> DeleteMailDeliveriesAsync(
        this AppDbContext context,
        DateTimeOffset sentBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM mail_deliveries
            WHERE (recipient_id, id) IN (
                SELECT recipient_id, id FROM mail_deliveries
                WHERE sent_at < {sentBefore}
                ORDER BY sent_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
