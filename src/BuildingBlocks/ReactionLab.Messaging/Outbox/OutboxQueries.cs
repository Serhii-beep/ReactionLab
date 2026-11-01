using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Messaging.Outbox;

internal static class OutboxQueries
{
    public static Task<List<OutboxMessage>> ListOldestOutboxMessagesAsync(
        this DbContext context,
        string topic,
        int count,
        CancellationToken cancellationToken) =>
        context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(message => message.Topic == topic)
            .OrderBy(message => message.Position)
            .Take(count)
            .ToListAsync(cancellationToken);

    public static async Task<DateTimeOffset?> FindOldestOutboxMessageTimeAsync(
        this DbContext context,
        string[] topics,
        CancellationToken cancellationToken) =>
        (await context.Database
            .SqlQuery<DateTimeOffset?>(
                $"""
                SELECT min(oldest.created_at) AS "Value"
                FROM unnest({topics}) AS published (topic)
                CROSS JOIN LATERAL (
                    SELECT outbox_messages.created_at
                    FROM outbox_messages
                    WHERE outbox_messages.topic = published.topic
                    ORDER BY outbox_messages.position
                    LIMIT 1) AS oldest
                """)
            .ToListAsync(cancellationToken))
            .SingleOrDefault();
}
