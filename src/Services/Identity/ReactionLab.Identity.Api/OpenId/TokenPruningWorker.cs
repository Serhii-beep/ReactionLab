using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class TokenPruningWorker(
    IServiceProvider services,
    TimeProvider timeProvider,
    ILogger<TokenPruningWorker> logger) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PruningInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RunBudget = TimeSpan.FromMinutes(5);

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
                await PruneAsync(run.Token);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Pruning OpenID tokens and sessions failed or ran out of time.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var startedAt = timeProvider.GetTimestamp();
        var now = timeProvider.GetUtcNow();

        var redeemed = await DeleteAllAsync(
            context,
            (db, token) => db.DeleteRedeemedTokensAsync(now - SessionPolicy.RedeemedTokenRetention, token),
            OpenIdPruningStatements.TokenBatchSize,
            cancellationToken);

        if (redeemed is null)
        {
            logger.LogInformation("Another instance is pruning OpenID tokens and sessions.");

            return;
        }

        var unused = await DeleteAllAsync(
            context,
            (db, token) => db.DeleteUnusedCodesAndRequestsAsync(now, token),
            OpenIdPruningStatements.TokenBatchSize,
            cancellationToken);

        var revoked = await DeleteAllAsync(
            context,
            (db, token) => db.DeleteRevokedSessionsAsync(token),
            OpenIdPruningStatements.SessionBatchSize,
            cancellationToken);

        var expired = await DeleteAllAsync(
            context,
            (db, token) => db.DeleteExpiredSessionsAsync(now - SessionPolicy.MaximumLifetime, token),
            OpenIdPruningStatements.SessionBatchSize,
            cancellationToken);

        logger.LogInformation(
            "Pruned {RedeemedCount} redeemed token(s), {UnusedCount} unused code(s) and request(s), {RevokedCount} revoked session(s) and {ExpiredCount} expired session(s) in {Elapsed}.",
            redeemed,
            unused,
            revoked,
            expired,
            timeProvider.GetElapsedTime(startedAt));
    }

    private static async Task<long?> DeleteAllAsync(
        AppDbContext context,
        Func<AppDbContext, CancellationToken, Task<int>> deleteBatch,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var total = 0L;

        while (await DeleteBatchAsync(context, deleteBatch, cancellationToken) is { } deleted)
        {
            total += deleted;

            if (deleted < batchSize)
            {
                return total;
            }
        }

        return null;
    }

    private static Task<int?> DeleteBatchAsync(
        AppDbContext context,
        Func<AppDbContext, CancellationToken, Task<int>> deleteBatch,
        CancellationToken cancellationToken) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            if (!await context.TryLockPruningAsync(cancellationToken))
            {
                return (int?)null;
            }

            var deleted = await deleteBatch(context, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return deleted;
        });
}
