using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal sealed class AccountRetentionWorker(
    IServiceProvider services,
    TimeProvider timeProvider,
    ILogger<AccountRetentionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RunBudget = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RunInterval, timeProvider);

        await Task.Delay(FirstRunDelay, timeProvider, stoppingToken);

        do
        {
            using var budget = new CancellationTokenSource(RunBudget, timeProvider);
            using var run = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, budget.Token);

            try
            {
                await ApplyRetentionAsync(run.Token);
            }
            catch (OperationCanceledException)
                when (budget.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Erasing never-confirmed accounts and pruning password records ran out of time. The next run "
                    + "carries on.");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Erasing never-confirmed accounts or pruning password records failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<OutboxWriter>();
        var startedAt = timeProvider.GetTimestamp();

        var erased = await RepeatAsync(
            _ => EraseBatchAsync(context, outbox),
            AccountRetentionStatements.ErasureBatchSize,
            cancellationToken);
        var changes = await RepeatAsync(
            context.DeleteExpiredPasswordChangesAsync,
            PasswordChangeStatements.PruningBatchSize,
            cancellationToken);
        var requests = await RepeatAsync(
            context.DeleteSpentPasswordResetRequestsAsync,
            SecurityLinkStatements.PruningBatchSize,
            cancellationToken);

        logger.LogInformation(
            "Erased {ErasedCount} account(s) never confirmed in time, and pruned {ChangeCount} password change(s) "
            + "and {RequestCount} spent reset request(s) in {Elapsed}.",
            erased,
            changes,
            requests,
            timeProvider.GetElapsedTime(startedAt));
    }

    private static Task<int> EraseBatchAsync(AppDbContext context, OutboxWriter outbox) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var erased = await context.EraseUnconfirmedAccountsAsync(SecurityLink.EmailConfirmationPeriod);

            foreach (var account in erased)
            {
                await outbox.InsertAsync(
                    context,
                    new UserErasureRequested(account.Id, account.Version + 1, OrganizationId: null));
                await outbox.InsertTombstoneAsync(context, IdentityTopics.Profiles, account.Id);
            }

            await transaction.CommitAsync(CancellationToken.None);

            return erased.Count;
        });

    private static async Task<long> RepeatAsync(
        Func<CancellationToken, Task<int>> deleteBatch,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var total = 0L;
        int deleted;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            deleted = await deleteBatch(cancellationToken);
            total += deleted;
        }
        while (deleted == batchSize);

        return total;
    }
}
