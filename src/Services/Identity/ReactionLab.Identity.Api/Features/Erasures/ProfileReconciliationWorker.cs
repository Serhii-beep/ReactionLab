using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Maintenance;

namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class ProfileReconciliationWorker(
    IServiceProvider services,
    ProfileReconciliation reconciliation,
    TimeProvider timeProvider,
    ILogger<ProfileReconciliationWorker> logger) : BackgroundService
{
    public const string MaintenanceRunName = "profile-reconciliation";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RunBudget = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan Lease = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileIfDueAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Checking whether identity.profiles is due for its reconciliation failed.");
            }
        }
    }

    private async Task ReconcileIfDueAsync(CancellationToken stoppingToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await context.TryLeaseMaintenanceRunAsync(MaintenanceRunName, Lease, stoppingToken) is not { } leasedUntil)
        {
            return;
        }

        var nextRunIn = RetryDelay;
        var startedAt = timeProvider.GetTimestamp();
        using var budget = new CancellationTokenSource(RunBudget, timeProvider);
        using var run = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, budget.Token);

        try
        {
            var reconciled = await reconciliation.RunAsync(run.Token);

            nextRunIn = RunInterval;
            logger.LogInformation(
                "Reconciled identity.profiles in {Elapsed}: {ProfileCount} profile(s) in {PartitionCount} "
                + "partition(s), {TombstonedCount} of them stale and now tombstoned.",
                timeProvider.GetElapsedTime(startedAt),
                reconciled.ProfileCount,
                reconciled.PartitionCount,
                reconciled.TombstonedCount);
        }
        catch (OperationCanceledException)
            when (budget.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Reconciling identity.profiles ran out of time. It runs again in {RetryDelay}.",
                RetryDelay);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Reconciling identity.profiles failed. It runs again in {RetryDelay}.", RetryDelay);
        }
        finally
        {
            await context.ReleaseMaintenanceRunAsync(MaintenanceRunName, leasedUntil, nextRunIn);
        }
    }
}
