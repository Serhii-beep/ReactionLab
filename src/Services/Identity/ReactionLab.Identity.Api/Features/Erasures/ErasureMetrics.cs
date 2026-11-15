using System.Diagnostics.Metrics;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;

namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class ErasureMetrics
{
    private static readonly double[] DurationBoundariesInSeconds = [1, 5, 30, 60, 300, 3600, 21600, 86400, 604800];

    private readonly Histogram<double> _duration;
    private int _overdueErasures;

    public ErasureMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(SignInMetrics.MeterName);

        _duration = meter.CreateHistogram<double>(
            "reactionlab.identity.erasure.duration",
            "s",
            "Time from an account's erasure request to the last service's confirmation.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DurationBoundariesInSeconds });

        meter.CreateObservableGauge(
            "reactionlab.identity.erasures.overdue",
            () => Volatile.Read(ref _overdueErasures),
            "{erasure}",
            "Erasures still waiting for a service's confirmation 7 days after their request, as last counted.");
    }

    public void RecordCompleted(TimeSpan duration) => _duration.Record(Math.Max(duration.TotalSeconds, 0));

    public void RecordOverdue(int count) => Volatile.Write(ref _overdueErasures, count);
}
