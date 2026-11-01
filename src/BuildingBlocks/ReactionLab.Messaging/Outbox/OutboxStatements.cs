using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Messaging.Outbox;

internal static class OutboxStatements
{
    private const int RelayLockClass = 0x6F757462;

    public static Task<int> InsertOutboxMessageAsync(this DbContext context, OutboxMessage message) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO outbox_messages (id, topic, subject, type, payload, trace_parent, created_at)
            VALUES (
                {message.Id},
                {message.Topic},
                {message.Subject},
                {message.Type},
                {message.Payload},
                {message.TraceParent},
                {message.CreatedAt})
            """,
            CancellationToken.None);

    public static async Task<bool> TryLockOutboxTopicAsync(
        this DbContext context,
        string topic,
        CancellationToken cancellationToken)
    {
        var locked = await context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({RelayLockClass}, hashtext({topic})) AS \"Value\"")
            .SingleAsync(cancellationToken);

        if (locked)
        {
            await context.Database.ExecuteSqlAsync(
                $"SET LOCAL idle_in_transaction_session_timeout = '30s'",
                cancellationToken);
        }

        return locked;
    }

    public static Task<int> DeleteOutboxMessagesAsync(
        this DbContext context,
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken) =>
        context.Set<OutboxMessage>()
            .Where(message => messageIds.Contains(message.Id))
            .ExecuteDeleteAsync(cancellationToken);

    public static Task<int> DeleteSupersededOutboxMessagesAsync(this DbContext context, string topic, string subject) =>
        context.Set<OutboxMessage>()
            .Where(message => message.Topic == topic && message.Subject == subject)
            .ExecuteDeleteAsync(CancellationToken.None);
}
