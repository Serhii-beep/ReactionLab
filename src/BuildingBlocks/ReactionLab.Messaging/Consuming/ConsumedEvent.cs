using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ReactionLab.Messaging.Consuming;

public sealed record ConsumedEvent(
    Guid Id,
    string Topic,
    string Type,
    string Subject,
    DateTimeOffset OccurredAt,
    ReadOnlyMemory<byte> Payload,
    string? TraceParent)
{
    public TEvent Read<TEvent>(JsonTypeInfo<TEvent> jsonTypeInfo)
    {
        TEvent? read;

        try
        {
            read = JsonSerializer.Deserialize(Payload.Span, jsonTypeInfo);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"{Type} doesn't parse at byte {ex.BytePositionInLine}.");
        }

        return read ?? throw new JsonException($"{Type} has a null payload.");
    }

    internal long ReadVersion() => Read(ConsumingJsonContext.Default.EventVersion).Version;
}
