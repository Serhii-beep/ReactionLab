using System.Diagnostics.Metrics;

namespace ReactionLab.Notifications.Sending;

internal sealed class MailMetrics
{
    private static readonly double[] DelayBoundariesInSeconds = [1, 2.5, 5, 10, 30, 60, 300, 900, 3600, 14400];

    private readonly Counter<long> _finished;
    private readonly Histogram<double> _delay;

    public MailMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MailTelemetry.SourceName);

        _finished = meter.CreateCounter<long>(
            "reactionlab.mail.jobs",
            "{job}",
            "Mail jobs the worker took up, by outcome.");

        _delay = meter.CreateHistogram<double>(
            "reactionlab.mail.delay",
            "s",
            "Time from a mail job's creation to the SMTP server accepting its mail.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DelayBoundariesInSeconds });
    }

    public void RecordFinished(string mailType, string outcome) =>
        _finished.Add(
            1,
            new KeyValuePair<string, object?>(MailTelemetry.MailTypeTag, mailType),
            new KeyValuePair<string, object?>(MailTelemetry.OutcomeTag, outcome));

    public void RecordSent(string mailType, TimeSpan delay) =>
        _delay.Record(
            Math.Max(delay.TotalSeconds, 0),
            new KeyValuePair<string, object?>(MailTelemetry.MailTypeTag, mailType));
}
