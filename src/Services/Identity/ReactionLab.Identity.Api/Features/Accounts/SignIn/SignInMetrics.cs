using System.Diagnostics.Metrics;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class SignInMetrics
{
    public const string MeterName = "ReactionLab.Identity";

    private readonly Counter<long> _slowFailures;

    public SignInMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _slowFailures = meter.CreateCounter<long>(
            "reactionlab.identity.sign_in.slow_failures",
            description: "Failed sign-ins that outlasted the minimum failure duration, so their timing could differ.");
    }

    public void RecordSlowFailure() => _slowFailures.Add(1);
}
