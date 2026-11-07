using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReactionLab.Messaging.Consuming;

public sealed class ConsumerPruningWorker<TContext>(
    IServiceProvider services,
    IOptions<EventConsumerOptions> options,
    TimeProvider timeProvider,
    ILogger<ConsumerPruningWorker<TContext>> logger) : PruningWorker(timeProvider, logger)
    where TContext : DbContext
{
    protected override async Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var keptSince = now - options.Value.Retention;
        string[] keptEventTypes = [.. options.Value.ErasureEventTypes];

        var processedEvents = await DeleteAllAsync(
            token => context.DeleteProcessedEventsAsync(keptSince, token),
            cancellationToken);

        var erasedDeadLetters = await DeleteAllAsync(context.DeleteErasedDeadLettersAsync, cancellationToken);

        var erasureMarkers = await DeleteAllAsync(
            token => context.DeleteErasureMarkersAsync(keptSince, token),
            cancellationToken);

        var deadLetters = await DeleteAllAsync(
            token => context.DeleteDeadLettersAsync(keptSince, keptEventTypes, token),
            cancellationToken);

        if (processedEvents + erasedDeadLetters + erasureMarkers + deadLetters > 0)
        {
            Logger.LogInformation(
                "Pruned {ProcessedEventCount} processed event(s), {ErasureMarkerCount} erasure marker(s) "
                + "and {DeadLetterCount} dead letter(s).",
                processedEvents,
                erasureMarkers,
                erasedDeadLetters + deadLetters);
        }
    }
}
