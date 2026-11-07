using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Messaging.Consuming;

public sealed class DeadLetterReplay<TContext>(
    IServiceProvider services,
    TimeProvider timeProvider,
    ILogger<DeadLetterReplay<TContext>> logger)
    where TContext : DbContext
{
    public const string CommandName = "replay-dead-letters";

    public async Task<bool> ReplayAllAsync(CancellationToken cancellationToken)
    {
        var failed = 0;
        var deadLetterIds = await ListDeadLetterIdsAsync(cancellationToken);

        foreach (var deadLetterId in deadLetterIds)
        {
            try
            {
                await ReplayAsync(deadLetterId, cancellationToken);
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                logger.LogError(ex, "Dead letter {DeadLetterId} failed again.", deadLetterId);
            }
        }

        logger.LogInformation(
            "Replayed {ReplayedCount} of {DeadLetterCount} dead letter(s).",
            deadLetterIds.Count - failed,
            deadLetterIds.Count);

        return failed == 0;
    }

    private async Task<List<Guid>> ListDeadLetterIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        return await context.Set<DeadLetter>()
            .OrderBy(letter => letter.FailedAt)
            .Select(letter => letter.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task ReplayAsync(Guid deadLetterId, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var effects = scope.ServiceProvider.GetRequiredService<IConsumedEventEffects>();

        context.Database.AutoSavepointsEnabled = false;

        await context.Database.CreateExecutionStrategy().ExecuteAsync(
            async cancellation =>
            {
                context.ChangeTracker.Clear();

                await using var transaction = await context.Database.BeginTransactionAsync(cancellation);

                if (await context.LockDeadLetterAsync(deadLetterId, cancellation) is not { } letter)
                {
                    return;
                }

                var consumed = letter.ToConsumedEvent()
                    ?? throw new InvalidOperationException("A malformed message can't be replayed.");

                if (await context.RememberEventAsync(consumed, timeProvider.GetUtcNow(), cancellation))
                {
                    await effects.ApplyAsync(consumed, cancellation);
                }

                await context.DeleteDeadLetterAsync(deadLetterId, cancellation);
                await transaction.CommitAsync(CancellationToken.None);
            },
            cancellationToken);
    }
}
