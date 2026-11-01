using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactionLab.Messaging.Kafka;

namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxRelay<TContext>(
    IServiceProvider services,
    IProducer<string, byte[]?> producer,
    KafkaProducerHealth producerHealth,
    PublishedEventTypes eventTypes,
    IOptions<OutboxRelayOptions> options,
    OutboxMetrics metrics,
    TimeProvider timeProvider,
    ILogger<OutboxRelay<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan BrokerProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly Dictionary<string, DateTimeOffset> _retryAtByTopic = [];
    private bool _brokerUnconfirmed = true;
    private bool _parked;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var brokerProbe = new DependentAdminClientBuilder(producer.Handle).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (producerHealth.FatalError is { } fatalError)
            {
                StopOrPark(fatalError);
            }

            await RecordOldestMessageAgeAsync(stoppingToken);

            var fullBatch = !_parked && await PublishTopicsAsync(brokerProbe, stoppingToken);

            if (!fullBatch)
            {
                await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
            }
        }
    }

    private void StopOrPark(Error fatalError)
    {
        if (producerHealth.RestartClearsFatalError)
        {
            logger.LogCritical(
                "The Kafka producer failed fatally ({Reason}). The instance stops to start a new one.",
                fatalError.Reason);

            throw new KafkaException(fatalError);
        }

        if (!_parked)
        {
            _parked = true;
            logger.LogCritical(
                "The Kafka producer can't publish ({Reason}), and a restart won't fix that. The relay stops "
                + "until Kafka's configuration is fixed and the instance restarts.",
                fatalError.Reason);
        }
    }

    private async Task<bool> PublishTopicsAsync(IAdminClient brokerProbe, CancellationToken stoppingToken)
    {
        if (_brokerUnconfirmed && !BrokerAnswers(brokerProbe))
        {
            return false;
        }

        _brokerUnconfirmed = false;
        var fullBatch = false;

        foreach (var topic in eventTypes.Topics)
        {
            if (_retryAtByTopic.TryGetValue(topic, out var retryAt) && retryAt > timeProvider.GetUtcNow())
            {
                continue;
            }

            try
            {
                fullBatch |= await PublishOldestAsync(topic, stoppingToken) == options.Value.BatchSize;
                _retryAtByTopic.Remove(topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                _brokerUnconfirmed = true;
                _retryAtByTopic[topic] = timeProvider.GetUtcNow() + options.Value.TopicRetryDelay;
                logger.LogError(
                    ex,
                    "The outbox relay failed to publish to {Topic}. It tries again in {RetryDelay}.",
                    topic,
                    options.Value.TopicRetryDelay);

                return false;
            }
        }

        return fullBatch;
    }

    private bool BrokerAnswers(IAdminClient brokerProbe)
    {
        try
        {
            brokerProbe.GetMetadata(eventTypes.Topics[0], BrokerProbeTimeout);

            return true;
        }
        catch (KafkaException)
        {
            return false;
        }
    }

    private async Task RecordOldestMessageAgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            var oldest = await context.FindOldestOutboxMessageTimeAsync([.. eventTypes.Topics], cancellationToken);

            metrics.RecordOldestMessageAge(
                oldest is { } createdAt ? timeProvider.GetUtcNow() - createdAt : TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "The outbox relay could not read the age of the outbox's oldest message.");
        }
    }

    private async Task<int> PublishOldestAsync(string topic, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        return await context.Database.CreateExecutionStrategy().ExecuteAsync(
            async cancellation =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellation);

                if (!await context.TryLockOutboxTopicAsync(topic, cancellation))
                {
                    return 0;
                }

                var messages = await context.ListOldestOutboxMessagesAsync(
                    topic,
                    options.Value.BatchSize,
                    cancellation);

                if (messages.Count == 0)
                {
                    return 0;
                }

                var failures = await Task.WhenAll(messages.Select(PublishAsync));
                var failedSubjects = messages
                    .Where((_, index) => failures[index] is not null)
                    .Select(message => message.Subject)
                    .ToHashSet();
                var deliveredIds = messages
                    .Where(message => !failedSubjects.Contains(message.Subject))
                    .Select(message => message.Id)
                    .ToList();

                await context.DeleteOutboxMessagesAsync(deliveredIds, CancellationToken.None);
                await transaction.CommitAsync(CancellationToken.None);
                metrics.RecordPublished(topic, deliveredIds.Count);

                if (failures.FirstOrDefault(failure => failure is not null) is { } firstFailure)
                {
                    throw firstFailure;
                }

                return messages.Count;
            },
            cancellationToken);
    }

    private async Task<KafkaException?> PublishAsync(OutboxMessage message)
    {
        using var activity = MessagingTelemetry.StartRelay(message);

        try
        {
            await producer.ProduceAsync(
                message.Topic,
                CloudEventMessages.FromOutbox(message, options.Value.CloudEventSource),
                CancellationToken.None);

            return null;
        }
        catch (KafkaException ex)
        {
            return ex;
        }
    }
}
