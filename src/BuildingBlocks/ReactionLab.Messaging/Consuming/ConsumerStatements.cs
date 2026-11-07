using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Messaging.Consuming;

public static class ConsumerStatements
{
    public static async Task RecordErasureAsync(
        this DbContext context,
        string subject,
        long version,
        DateTimeOffset erasedAt,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO erasure_markers (subject, version, erased_at)
            VALUES ({subject}, {version}, {erasedAt})
            ON CONFLICT (subject) DO UPDATE
            SET version = greatest(erasure_markers.version, excluded.version), erased_at = excluded.erased_at
            """,
            cancellationToken);

        await context.Set<DeadLetter>()
            .Where(letter => letter.Subject == subject)
            .ExecuteDeleteAsync(cancellationToken);
    }

    internal static Task<int> StartTransactionOnServerAsync(
        this DbContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync($"SELECT 1", cancellationToken);

    internal static async Task<bool> RememberEventAsync(
        this DbContext context,
        ConsumedEvent consumed,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken)
    {
        var applies = await context.Database
            .SqlQuery<bool>(
                $"""
                WITH remembered AS (
                    INSERT INTO processed_events (event_id, processed_at)
                    VALUES ({consumed.Id}, {processedAt})
                    ON CONFLICT (event_id) DO NOTHING
                    RETURNING event_id)
                SELECT EXISTS (SELECT FROM remembered)
                    AND NOT EXISTS (
                        SELECT FROM erasure_markers
                        WHERE subject = {consumed.Subject} AND version >= {consumed.ReadVersion()}) AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return applies.Single();
    }

    internal static Task<DeadLetter?> LockDeadLetterAsync(
        this DbContext context,
        Guid id,
        CancellationToken cancellationToken) =>
        context.Set<DeadLetter>()
            .FromSql($"SELECT * FROM dead_letters WHERE id = {id} FOR UPDATE SKIP LOCKED")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    internal static Task<int> DeleteDeadLetterAsync(
        this DbContext context,
        Guid id,
        CancellationToken cancellationToken) =>
        context.Set<DeadLetter>().Where(letter => letter.Id == id).ExecuteDeleteAsync(cancellationToken);

    internal static Task<int> DeleteProcessedEventsAsync(
        this DbContext context,
        DateTimeOffset processedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM processed_events
            WHERE event_id IN (
                SELECT event_id FROM processed_events
                WHERE processed_at < {processedBefore}
                ORDER BY processed_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    internal static Task<int> DeleteErasedDeadLettersAsync(
        this DbContext context,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM dead_letters
            WHERE id IN (
                SELECT letter.id
                FROM dead_letters AS letter
                JOIN erasure_markers AS marker ON marker.subject = letter.subject
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE OF letter SKIP LOCKED)
            """,
            cancellationToken);

    internal static Task<int> DeleteErasureMarkersAsync(
        this DbContext context,
        DateTimeOffset erasedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM erasure_markers
            WHERE subject IN (
                SELECT subject FROM erasure_markers
                WHERE erased_at < {erasedBefore}
                ORDER BY erased_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);

    internal static Task<int> DeleteDeadLettersAsync(
        this DbContext context,
        DateTimeOffset failedBefore,
        string[] keptEventTypes,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM dead_letters
            WHERE id IN (
                SELECT id FROM dead_letters
                WHERE failed_at < {failedBefore} AND (type IS NULL OR type <> ALL({keptEventTypes}))
                ORDER BY failed_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
