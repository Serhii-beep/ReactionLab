using Confluent.Kafka;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class ProfileReconciliation(
    IServiceProvider services,
    IProducer<string, byte[]?> producer,
    IConfiguration configuration,
    ILogger<ProfileReconciliation> logger)
{
    public const string GroupId = "identity-profile-reconciliation";

    private const string KafkaConnectionName = "kafka";
    private const string ConsumerConfigSection = "Aspire:Confluent:Kafka:Consumer:Config";
    private const int BatchSize = 1_000;

    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(10);

    public async Task<ReconciledProfiles> RunAsync(CancellationToken cancellationToken)
    {
        using var consumer = new ConsumerBuilder<string, byte[]?>(CreateConsumerConfig())
            .SetErrorHandler((_, error) => logger.LogWarning(
                "The reconciliation's Kafka consumer reported {ErrorCode}: {Reason}",
                error.Code,
                error.Reason))
            .SetLogHandler((_, message) => logger.Log(
                LogLevelOf(message.Level),
                "The reconciliation's Kafka consumer logged {Facility}: {Message}",
                message.Facility,
                message.Message))
            .Build();
        using var metadataClient = new DependentAdminClientBuilder(consumer.Handle).Build();

        var topic = metadataClient.GetMetadata(IdentityTopics.Profiles, MetadataTimeout).Topics.Single();

        if (topic.Error.IsError)
        {
            throw new KafkaException(topic.Error);
        }

        var profileCount = 0L;
        var tombstonedCount = 0;

        foreach (var partition in topic.Partitions.OrderBy(partition => partition.PartitionId))
        {
            var topicPartition = new TopicPartition(IdentityTopics.Profiles, new Partition(partition.PartitionId));
            var profiles = ReadPartitionProfiles(consumer, topicPartition, cancellationToken);

            profileCount += profiles.Count;
            tombstonedCount += await TombstoneStaleProfilesAsync(topicPartition, profiles, cancellationToken);
        }

        consumer.Close();

        return new ReconciledProfiles(topic.Partitions.Count, profileCount, tombstonedCount);
    }

    private static LogLevel LogLevelOf(SyslogLevel level) =>
        level switch
        {
            SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
            SyslogLevel.Warning => LogLevel.Warning,
            SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
            _ => LogLevel.Debug
        };

    private static PartitionProfiles ReadPartitionProfiles(
        IConsumer<string, byte[]?> consumer,
        TopicPartition topicPartition,
        CancellationToken cancellationToken)
    {
        var profiles = new PartitionProfiles();
        var watermarks = consumer.QueryWatermarkOffsets(topicPartition, MetadataTimeout);

        if (watermarks.High <= watermarks.Low)
        {
            return profiles;
        }

        consumer.Assign(new TopicPartitionOffset(topicPartition, watermarks.Low));

        try
        {
            while (consumer.Consume(cancellationToken) is { IsPartitionEOF: false } result)
            {
                if (Guid.TryParse(result.Message.Key, out var accountId) && accountId != Guid.Empty)
                {
                    profiles.Record(result.Message.Key, accountId, result.Message.Value is not null);
                }

                if (result.Offset.Value >= watermarks.High.Value - 1)
                {
                    break;
                }
            }
        }
        finally
        {
            consumer.Unassign();
        }

        return profiles;
    }

    private async Task<int> TombstoneStaleProfilesAsync(
        TopicPartition topicPartition,
        PartitionProfiles profiles,
        CancellationToken cancellationToken)
    {
        var tombstonedCount = 0;

        foreach (var batch in profiles.OtherKeys.Chunk(BatchSize))
        {
            tombstonedCount += await ProduceTombstonesAsync(topicPartition, batch, cancellationToken);
        }

        foreach (var batch in profiles.CanonicalAccountIds.Chunk(BatchSize))
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingAccountIds = await context.ListExistingAccountIdsAsync(batch, cancellationToken);
            string[] staleKeys = [.. batch.Except(existingAccountIds).Select(accountId => accountId.ToString())];

            tombstonedCount += await ProduceTombstonesAsync(topicPartition, staleKeys, cancellationToken);
        }

        return tombstonedCount;
    }

    private async Task<int> ProduceTombstonesAsync(
        TopicPartition topicPartition,
        string[] keys,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(keys.Select(key => producer.ProduceAsync(
            topicPartition,
            new Message<string, byte[]?> { Key = key, Value = null },
            cancellationToken)));

        return keys.Length;
    }

    private ConsumerConfig CreateConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString(KafkaConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{KafkaConnectionName}' is not configured.")
        };

        configuration.GetSection(ConsumerConfigSection).Bind(config);

        config.GroupId = GroupId;
        config.EnableAutoCommit = false;
        config.EnableAutoOffsetStore = false;
        config.EnablePartitionEof = true;
        config.AllowAutoCreateTopics = false;

        return config;
    }
}
