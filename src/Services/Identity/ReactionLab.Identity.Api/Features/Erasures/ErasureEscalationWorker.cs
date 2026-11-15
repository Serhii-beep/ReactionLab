using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Erasures;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class ErasureEscalationWorker(
    IServiceProvider services,
    ErasureMetrics metrics,
    TimeProvider timeProvider,
    ILogger<ErasureEscalationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);
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
                await CheckErasuresAsync(run.Token);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Escalating overdue erasures or pruning completed ones failed or ran out of time.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckErasuresAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<OutboxWriter>();
        var now = timeProvider.GetUtcNow();
        var overdueBefore = now - AccountErasure.ConfirmationPeriod;

        await EscalateAsync(context, outbox, overdueBefore, now, cancellationToken);

        metrics.RecordOverdue(await context.CountOverdueErasuresAsync(overdueBefore, cancellationToken));

        var pruned = 0L;
        int deleted;

        do
        {
            deleted = await context.DeleteExpiredErasuresAsync(
                now.AddYears(-AccountErasure.RetentionYears),
                cancellationToken);
            pruned += deleted;
        }
        while (deleted == AccountErasureStatements.PruningBatchSize);

        if (pruned > 0)
        {
            logger.LogInformation("Pruned {ErasureCount} completed erasure record(s) past their retention.", pruned);
        }
    }

    private async Task EscalateAsync(
        AppDbContext context,
        OutboxWriter outbox,
        DateTimeOffset overdueBefore,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var escalatedCount = 0;
        var oldestRequestedAt = DateTimeOffset.MaxValue;
        var pendingServices = new SortedSet<string>(StringComparer.Ordinal);
        List<EscalatedErasure> escalated;

        try
        {
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                escalated = await EscalateBatchAsync(context, outbox, overdueBefore, now);
                escalatedCount += escalated.Count;

                foreach (var erasure in escalated)
                {
                    oldestRequestedAt = erasure.RequestedAt < oldestRequestedAt ? erasure.RequestedAt : oldestRequestedAt;

                    if (erasure.PendingServices is { } pending)
                    {
                        pendingServices.UnionWith(pending.Split(", "));
                    }
                }
            }
            while (escalated.Count == AccountErasureStatements.EscalationBatchSize);
        }
        finally
        {
            if (escalatedCount > 0)
            {
                logger.LogError(
                    "{ErasureCount} erasure(s) still lack a confirmation from {Services} {ConfirmationPeriod} after "
                    + "their request or their last escalation, so each is requested again. The oldest was requested "
                    + "at {OldestRequestedAt}.",
                    escalatedCount,
                    string.Join(", ", pendingServices),
                    AccountErasure.ConfirmationPeriod,
                    oldestRequestedAt);
            }
        }
    }

    private static Task<List<EscalatedErasure>> EscalateBatchAsync(
        AppDbContext context,
        OutboxWriter outbox,
        DateTimeOffset overdueBefore,
        DateTimeOffset now) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            await context.StartTransactionOnServerAsync(CancellationToken.None);

            var batch = await context.EscalateOverdueErasuresAsync(overdueBefore, now, CancellationToken.None);

            await outbox.InsertAllAsync(context, batch.Select(erasure =>
                new UserErasureRequested(erasure.UserId, erasure.Version, erasure.OrganizationId)));
            await transaction.CommitAsync(CancellationToken.None);

            return batch;
        });
}
