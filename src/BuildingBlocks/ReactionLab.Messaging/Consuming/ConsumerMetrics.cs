using System.Diagnostics.Metrics;

namespace ReactionLab.Messaging.Consuming;

public sealed class ConsumerMetrics
{
    private static readonly double[] DelayBoundariesInSeconds = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 300];

    private readonly Counter<long> _consumed;
    private readonly Counter<long> _deadLettered;
    private readonly Histogram<double> _delay;

    public ConsumerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);

        _consumed = meter.CreateCounter<long>(
            "reactionlab.consumer.consumed",
            "{event}",
            "Events the consumer applied, or dropped as duplicates or as older than an erasure.");

        _deadLettered = meter.CreateCounter<long>(
            "reactionlab.consumer.dead_lettered",
            "{message}",
            "Messages the consumer moved to its dead letters.");

        _delay = meter.CreateHistogram<double>(
            "reactionlab.consumer.delay",
            "s",
            "Time from an event's creation to the commit of its effect.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DelayBoundariesInSeconds });
    }

    public void RecordConsumed(ConsumedEvent consumed, bool applied, TimeSpan delay)
    {
        _consumed.Add(
            1,
            new KeyValuePair<string, object?>("messaging.destination.name", consumed.Topic),
            new KeyValuePair<string, object?>("cloudevents.event_type", consumed.Type),
            new KeyValuePair<string, object?>("reactionlab.consumer.applied", applied));

        if (applied)
        {
            _delay.Record(
                Math.Max(delay.TotalSeconds, 0),
                new KeyValuePair<string, object?>("messaging.destination.name", consumed.Topic));
        }
    }

    public void RecordDeadLettered(string topic) =>
        _deadLettered.Add(1, new KeyValuePair<string, object?>("messaging.destination.name", topic));
}
