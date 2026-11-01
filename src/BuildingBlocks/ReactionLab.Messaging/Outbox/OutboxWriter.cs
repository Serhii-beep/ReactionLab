using Microsoft.EntityFrameworkCore;
using ReactionLab.SharedKernel;

namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxWriter(PublishedEventTypes eventTypes, TimeProvider timeProvider)
{
    public Task<int> InsertAsync(DbContext context, IDomainEvent domainEvent)
    {
        RequireTransaction(context);

        return context.InsertOutboxMessageAsync(eventTypes.ToOutboxMessage(
            domainEvent,
            MessagingTelemetry.CurrentTraceParent(),
            timeProvider.GetUtcNow()));
    }

    public async Task<int> InsertTombstoneAsync(DbContext context, string topic, Guid subject)
    {
        RequireTransaction(context);

        if (!eventTypes.StateTopics.Contains(topic))
        {
            throw new InvalidOperationException($"{topic} is not a state topic this service publishes to.");
        }

        await context.DeleteSupersededOutboxMessagesAsync(topic, subject.ToString());

        return await context.InsertOutboxMessageAsync(OutboxMessage.ForTombstone(
            topic,
            subject.ToString(),
            MessagingTelemetry.CurrentTraceParent(),
            timeProvider.GetUtcNow()));
    }

    private static void RequireTransaction(DbContext context)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("An outbox message is written inside the transaction of its change.");
        }
    }
}
