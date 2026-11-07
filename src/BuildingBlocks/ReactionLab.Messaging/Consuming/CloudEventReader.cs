using System.Diagnostics;
using System.Globalization;
using System.Text;
using Confluent.Kafka;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging.Consuming;

internal static class CloudEventReader
{
    public static ConsumedEvent? TryRead(ConsumeResult<string, byte[]?> result)
    {
        var headers = result.Message.Headers;

        if (ReadStorablePayload(result) is not { } payload
            || ReadStorableSubject(result) is not { } subject
            || ReadStorableType(result) is not { } type
            || !Guid.TryParse(ReadHeader(headers, "ce_id"), out var id)
            || !TryReadTime(ReadHeader(headers, "ce_time"), out var occurredAt))
        {
            return null;
        }

        return new ConsumedEvent(id, result.Topic, type, subject, occurredAt, payload, ReadTraceParent(headers));
    }

    public static string? ReadStorableSubject(ConsumeResult<string, byte[]?> result) =>
        IsStorable(result.Message.Key, OutboxMessage.MaximumSubjectLength) ? result.Message.Key : null;

    public static string? ReadStorableType(ConsumeResult<string, byte[]?> result) =>
        ReadHeader(result.Message.Headers, "ce_type") is { } type && IsStorable(type, OutboxMessage.MaximumTypeLength)
            ? type
            : null;

    public static byte[]? ReadStorablePayload(ConsumeResult<string, byte[]?> result) =>
        result.Message.Value is { Length: <= PublishedEventTypes.MaximumPayloadBytes } payload ? payload : null;

    private static string? ReadHeader(Headers? headers, string name) =>
        headers is not null && headers.TryGetLastBytes(name, out var value) && value is not null
            ? Encoding.UTF8.GetString(value)
            : null;

    private static bool IsStorable(string? text, int maximumLength) =>
        !string.IsNullOrEmpty(text) && text.Length <= maximumLength && !text.Contains('\0');

    private static bool TryReadTime(string? text, out DateTimeOffset occurredAt) =>
        DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out occurredAt);

    private static string? ReadTraceParent(Headers? headers)
    {
        var traceParent = ReadHeader(headers, "traceparent");

        return IsStorable(traceParent, OutboxMessage.TraceParentLength)
            && ActivityContext.TryParse(traceParent, null, out _)
                ? traceParent
                : null;
    }
}
