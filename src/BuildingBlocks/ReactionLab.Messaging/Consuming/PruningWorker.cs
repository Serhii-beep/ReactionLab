using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Messaging.Consuming;

public abstract class PruningWorker(TimeProvider timeProvider, ILogger logger) : BackgroundService
{
    public const int BatchSize = 1_000;

    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PruningInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RunBudget = TimeSpan.FromMinutes(5);

    protected ILogger Logger => logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PruningInterval, timeProvider);

        await Task.Delay(FirstRunDelay, timeProvider, stoppingToken);

        do
        {
            using var budget = new CancellationTokenSource(RunBudget, timeProvider);
            using var run = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, budget.Token);

            try
            {
                await PruneAsync(timeProvider.GetUtcNow(), run.Token);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Pruning failed or ran out of time.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    protected abstract Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken);

    protected static async Task<long> DeleteAllAsync(
        Func<CancellationToken, Task<int>> deleteBatch,
        CancellationToken cancellationToken)
    {
        var total = 0L;
        int deleted;

        do
        {
            deleted = await deleteBatch(cancellationToken);
            total += deleted;
        }
        while (deleted == BatchSize);

        return total;
    }
}
