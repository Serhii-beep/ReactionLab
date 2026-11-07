using Confluent.Kafka;

namespace ReactionLab.Messaging.Consuming;

public sealed class DeadLetter
{
    private DeadLetter()
    {

    }

    public Guid Id { get; private init; }

    public string Topic { get; private init; } = string.Empty;

    public int Partition { get; private init; }

    public long Offset { get; private init; }

    public Guid? EventId { get; private init; }

    public string? Type { get; private init; }

    public string? Subject { get; private init; }

    public DateTimeOffset? OccurredAt { get; private init; }

    public byte[]? Payload { get; private init; }

    public string? TraceParent { get; private init; }

    public string Failure { get; private init; } = string.Empty;

    public DateTimeOffset FailedAt { get; private init; }

    public ConsumedEvent? ToConsumedEvent() =>
        (EventId, Type, Subject, OccurredAt, Payload) is ({ } id, { } type, { } subject, { } occurredAt, { } payload)
            ? new ConsumedEvent(id, Topic, type, subject, occurredAt, payload, TraceParent)
            : null;

    internal static DeadLetter Create(
        ConsumeResult<string, byte[]?> result,
        ConsumedEvent? consumed,
        string failure,
        DateTimeOffset failedAt) =>
        new()
        {
            Id = Guid.CreateVersion7(failedAt),
            Topic = result.Topic,
            Partition = result.Partition.Value,
            Offset = result.Offset.Value,
            EventId = consumed?.Id,
            Type = CloudEventReader.ReadStorableType(result),
            Subject = CloudEventReader.ReadStorableSubject(result),
            OccurredAt = consumed?.OccurredAt,
            Payload = CloudEventReader.ReadStorablePayload(result),
            TraceParent = consumed?.TraceParent,
            Failure = failure,
            FailedAt = failedAt
        };
}
