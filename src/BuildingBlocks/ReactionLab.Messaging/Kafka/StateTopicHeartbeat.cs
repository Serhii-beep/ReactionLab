using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Messaging.Kafka;

public sealed class StateTopicHeartbeat(
    IProducer<string, byte[]?> producer,
    KafkaProducerHealth producerHealth,
    PublishedEventTypes eventTypes,
    IOptions<OutboxRelayOptions> options,
    TimeProvider timeProvider,
    ILogger<StateTopicHeartbeat> logger) : BackgroundService
{
    public static readonly string HeartbeatKey = Guid.Empty.ToString();

    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (eventTypes.StateTopics.Count == 0)
        {
            return;
        }

        using var metadataClient = new DependentAdminClientBuilder(producer.Handle).Build();
        using var timer = new PeriodicTimer(options.Value.StateTopicHeartbeatInterval, timeProvider);

        do
        {
            if (producerHealth.FatalError is null)
            {
                await BeatEveryStateTopicAsync(metadataClient, stoppingToken);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task BeatEveryStateTopicAsync(IAdminClient metadataClient, CancellationToken stoppingToken)
    {
        foreach (var topic in eventTypes.StateTopics)
        {
            try
            {
                await BeatAsync(metadataClient, topic);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "The heartbeat could not reach every partition of {Topic}.", topic);
            }
        }
    }

    private async Task BeatAsync(IAdminClient metadataClient, string topic)
    {
        var topicMetadata = metadataClient.GetMetadata(topic, MetadataTimeout).Topics.Single();

        if (topicMetadata.Error.IsError)
        {
            throw new KafkaException(topicMetadata.Error);
        }

        await Task.WhenAll(topicMetadata.Partitions.Select(partition => producer.ProduceAsync(
            new TopicPartition(topic, new Partition(partition.PartitionId)),
            new Message<string, byte[]?> { Key = HeartbeatKey, Value = null },
            CancellationToken.None)));
    }
}
