namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxMessage
{
    public const int MaximumTopicLength = 249;
    public const int MaximumSubjectLength = 64;
    public const int MaximumTypeLength = 128;
    public const int TraceParentLength = 55;

    private OutboxMessage()
    {

    }

    public Guid Id { get; private init; }

    public long Position { get; private init; }

    public string Topic { get; private init; } = string.Empty;

    public string Subject { get; private init; } = string.Empty;

    public string? Type { get; private init; }

    public string? Payload { get; private init; }

    public string? TraceParent { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static OutboxMessage ForEvent(
        Guid eventId,
        string topic,
        string subject,
        string type,
        string payload,
        string? traceParent,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = eventId,
            Topic = topic,
            Subject = subject,
            Type = type,
            Payload = payload,
            TraceParent = traceParent,
            CreatedAt = createdAt
        };

    public static OutboxMessage ForTombstone(
        string topic,
        string subject,
        string? traceParent,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(createdAt),
            Topic = topic,
            Subject = subject,
            TraceParent = traceParent,
            CreatedAt = createdAt
        };
}
