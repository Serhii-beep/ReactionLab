using System.Diagnostics.Metrics;

namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxMetrics
{
    private readonly Counter<long> _published;
    private double _oldestMessageAgeSeconds;

    public OutboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);

        _published = meter.CreateCounter<long>(
            "reactionlab.outbox.published",
            "{message}",
            "Messages the outbox relay published to Kafka.");

        meter.CreateObservableGauge(
            "reactionlab.outbox.age",
            () => Volatile.Read(ref _oldestMessageAgeSeconds),
            "s",
            "Age of the oldest message waiting in the outbox, as the relay last saw it.");
    }

    public void RecordPublished(string topic, int count) =>
        _published.Add(count, new KeyValuePair<string, object?>("messaging.destination.name", topic));

    public void RecordOldestMessageAge(TimeSpan age) =>
        Volatile.Write(ref _oldestMessageAgeSeconds, age.TotalSeconds);
}
