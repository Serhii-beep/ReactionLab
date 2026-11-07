namespace ReactionLab.Messaging.Consuming;

public sealed class ProcessedEvent
{
    private ProcessedEvent()
    {

    }

    public Guid EventId { get; private init; }

    public DateTimeOffset ProcessedAt { get; private init; }
}
