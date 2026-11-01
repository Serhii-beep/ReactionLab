using System.Globalization;
using System.Text;
using Confluent.Kafka;

namespace ReactionLab.Messaging.Outbox;

internal static class CloudEventMessages
{
    private const string SpecVersion = "1.0";
    private const string JsonContentType = "application/json";

    public static Message<string, byte[]?> FromOutbox(OutboxMessage message, string cloudEventSource) =>
        new()
        {
            Key = message.Subject,
            Value = message.Payload is { } payload ? Encoding.UTF8.GetBytes(payload) : null,
            Headers = message.Type is { } type ? CloudEventHeaders(message, type, cloudEventSource) : new Headers()
        };

    private static Headers CloudEventHeaders(OutboxMessage message, string type, string cloudEventSource) =>
        new()
        {
            { "ce_specversion", Encoding.UTF8.GetBytes(SpecVersion) },
            { "ce_id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
            { "ce_source", Encoding.UTF8.GetBytes(cloudEventSource) },
            { "ce_type", Encoding.UTF8.GetBytes(type) },
            { "ce_time", Encoding.UTF8.GetBytes(message.CreatedAt.ToString("O", CultureInfo.InvariantCulture)) },
            { "ce_subject", Encoding.UTF8.GetBytes(message.Subject) },
            { "content-type", Encoding.UTF8.GetBytes(JsonContentType) },
        };
}
