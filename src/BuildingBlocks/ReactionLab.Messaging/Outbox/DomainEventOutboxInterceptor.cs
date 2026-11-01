using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReactionLab.SharedKernel;

namespace ReactionLab.Messaging.Outbox;

public sealed class DomainEventOutboxInterceptor(PublishedEventTypes eventTypes, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<IDomainEvent, OutboxMessage> _messagesByEvent = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            WriteDomainEvents(context);
        }

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            WriteDomainEvents(context);
        }

        return ValueTask.FromResult(result);
    }

    private void WriteDomainEvents(DbContext context)
    {
        var now = timeProvider.GetUtcNow();
        var traceParent = MessagingTelemetry.CurrentTraceParent();

        foreach (var source in context.ChangeTracker.Entries<IDomainEventSource>().ToList())
        {
            foreach (var domainEvent in source.Entity.DomainEvents)
            {
                if (_messagesByEvent.TryGetValue(domainEvent, out var written))
                {
                    if (source.State == EntityState.Added)
                    {
                        Reinsert(context.Entry(written));
                    }

                    continue;
                }

                var message = eventTypes.ToOutboxMessage(domainEvent, traceParent, now);
                _messagesByEvent.Add(domainEvent, message);
                context.Add(message);
            }
        }
    }

    private static void Reinsert(EntityEntry<OutboxMessage> message)
    {
        if (message.State is EntityState.Detached or EntityState.Unchanged)
        {
            message.State = EntityState.Added;
        }
    }
}
