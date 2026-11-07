using Microsoft.EntityFrameworkCore;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Notifications.Persistence.Notices;

public static class NoticeThrottleStatements
{
    public static async Task<bool> TryClaimNoticeAsync(
        this AppDbContext context,
        Guid recipientId,
        string mailType,
        DateTimeOffset queuedAt,
        TimeSpan interval,
        CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO notice_throttles (recipient_id, mail_type, last_queued_at)
            VALUES ({recipientId}, {mailType}, {queuedAt})
            ON CONFLICT (recipient_id, mail_type) DO UPDATE
            SET last_queued_at = excluded.last_queued_at
            WHERE notice_throttles.last_queued_at + {interval} <= excluded.last_queued_at
            """,
            cancellationToken) == 1;

    public static Task<int> DeleteNoticeThrottlesAsync(
        this AppDbContext context,
        DateTimeOffset queuedBefore,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM notice_throttles
            WHERE (recipient_id, mail_type) IN (
                SELECT recipient_id, mail_type FROM notice_throttles
                WHERE last_queued_at < {queuedBefore}
                ORDER BY last_queued_at
                LIMIT {PruningWorker.BatchSize}
                FOR UPDATE SKIP LOCKED)
            """,
            cancellationToken);
}
