using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed class CatalogReleaseHealthCheck(CatalogReleaseRegistry registry, TimeProvider timeProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = registry.State;

        if (state.PolledAt is not { } polledAt)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("The catalog releases have not been loaded yet."));
        }

        return Task.FromResult(timeProvider.GetUtcNow() - polledAt > CatalogReleasePublisher.InstanceTimeout
            ? HealthCheckResult.Unhealthy($"The catalog releases were last refreshed at {polledAt:O}.")
            : HealthCheckResult.Healthy($"Holding {state.HeldByReleaseId.Count} catalog release(s)."));
    }
}
