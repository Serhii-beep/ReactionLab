using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReactionLab.Messaging.Consuming;

public sealed class EventConsumer<TContext>(
    IServiceProvider services,
    IConsumer<string, byte[]?> consumer,
    IOptions<EventConsumerOptions> options,
    ConsumerMetrics metrics,
    TimeProvider timeProvider,
    ILogger<EventConsumer<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private const string MalformedMessage = "MalformedMessage";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(options.Value.Topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("The event consumer is stopping.");
        }
        catch (Exception) when (!stoppingToken.IsCancellationRequested)
        {
            Environment.ExitCode = 1;

            throw;
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ConsumeNextAsync(CancellationToken stoppingToken)
    {
        ConsumeResult<string, byte[]?> result;

        try
        {
            result = consumer.Consume(stoppingToken);
        }
        catch (ConsumeException ex) when (!ex.Error.IsFatal)
        {
            logger.LogWarning("The Kafka consumer reported {ErrorCode}: {Reason}", ex.Error.Code, ex.Error.Reason);
            await Task.Delay(options.Value.HoldDelay, timeProvider, stoppingToken);

            return;
        }

        if (await TryProcessAsync(result, stoppingToken))
        {
            StoreOffset(result);

            return;
        }

        Rewind(result);
        await Task.Delay(options.Value.HoldDelay, timeProvider, stoppingToken);
    }

    private async Task<bool> TryProcessAsync(
        ConsumeResult<string, byte[]?> result,
        CancellationToken stoppingToken)
    {
        if (CloudEventReader.TryRead(result) is not { } consumed)
        {
            return await TryDeadLetterAsync(result, null, MalformedMessage, stoppingToken);
        }

        if (!options.Value.EventTypes.Contains(consumed.Type))
        {
            return true;
        }

        using var activity = MessagingTelemetry.StartConsume(consumed);

        for (var retry = 0; ; retry++)
        {
            try
            {
                var applied = await ApplyAsync(consumed, stoppingToken);

                metrics.RecordConsumed(consumed, applied, timeProvider.GetUtcNow() - consumed.OccurredAt);

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                if (ex is JsonException || retry == options.Value.Retries)
                {
                    logger.LogError(
                        ex,
                        "Event {EventId} of type {EventType} failed terminally at {Topic} [{Partition}] @{Offset}.",
                        consumed.Id,
                        consumed.Type,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);

                    return await TryDeadLetterAsync(result, consumed, ex.GetType().FullName!, stoppingToken);
                }

                await Task.Delay(options.Value.FirstRetryDelay * (1 << retry), timeProvider, stoppingToken);
            }
        }
    }

    private async Task<bool> ApplyAsync(ConsumedEvent consumed, CancellationToken stoppingToken)
    {
        using var attempt = StartAttempt(stoppingToken);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var effects = scope.ServiceProvider.GetRequiredService<IConsumedEventEffects>();

        context.Database.AutoSavepointsEnabled = false;

        return await context.Database.CreateExecutionStrategy().ExecuteAsync(
            async cancellation =>
            {
                context.ChangeTracker.Clear();

                await using var transaction = await context.Database.BeginTransactionAsync(cancellation);

                await context.StartTransactionOnServerAsync(cancellation);

                var applies = await context.RememberEventAsync(consumed, timeProvider.GetUtcNow(), cancellation);

                if (applies)
                {
                    await effects.ApplyAsync(consumed, cancellation);
                }

                await transaction.CommitAsync(CancellationToken.None);

                return applies;
            },
            attempt.Token);
    }

    private async Task<bool> TryDeadLetterAsync(
        ConsumeResult<string, byte[]?> result,
        ConsumedEvent? consumed,
        string failure,
        CancellationToken stoppingToken)
    {
        using var attempt = StartAttempt(stoppingToken);

        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            context.Add(DeadLetter.Create(result, consumed, failure, timeProvider.GetUtcNow()));

            await context.SaveChangesAsync(attempt.Token);

            metrics.RecordDeadLettered(result.Topic);
            logger.LogError(
                "A dead letter holds {Topic} [{Partition}] @{Offset}, event {EventId}: {Failure}.",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value,
                consumed?.Id,
                failure);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "No dead letter could be written for {Topic} [{Partition}] @{Offset}. The partition holds its place.",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value);

            return false;
        }
    }

    private CancellationTokenSource StartAttempt(CancellationToken stoppingToken)
    {
        var attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        attempt.CancelAfter(options.Value.AttemptBudget);

        return attempt;
    }

    private void StoreOffset(ConsumeResult<string, byte[]?> result)
    {
        try
        {
            consumer.StoreOffset(result);
        }
        catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_State)
        {
            logger.LogInformation(
                "{Topic} [{Partition}] moved to another consumer before @{Offset} was stored.",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value);
        }
    }

    private void Rewind(ConsumeResult<string, byte[]?> result)
    {
        try
        {
            consumer.Seek(result.TopicPartitionOffset);
        }
        catch (KafkaException ex) when (!consumer.Assignment.Contains(result.TopicPartition))
        {
            logger.LogInformation(
                "{Topic} [{Partition}] moved to another consumer before it could rewind: {Reason}.",
                result.Topic,
                result.Partition.Value,
                ex.Error.Reason);
        }
    }
}
