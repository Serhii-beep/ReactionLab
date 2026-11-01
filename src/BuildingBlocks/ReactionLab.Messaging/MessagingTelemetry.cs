using System.Diagnostics;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging;

public static class MessagingTelemetry
{
    public const string SourceName = "ReactionLab.Messaging";

    private static readonly ActivitySource Source = new(SourceName);

    internal static string? CurrentTraceParent() =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity ? activity.Id : null;

    internal static Activity? StartRelay(OutboxMessage message)
    {
        if (!ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out var parent))
        {
            return null;
        }

        var activity = Source.StartActivity("outbox relay", ActivityKind.Internal, parent);
        activity?.SetTag("messaging.destination.name", message.Topic);
        activity?.SetTag("messaging.message.id", message.Id.ToString());

        return activity;
    }
}
