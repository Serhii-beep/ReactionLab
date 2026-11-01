using System.Globalization;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Kafka;

namespace ReactionLab.Identity.Migrator;

internal sealed class IdentityTopicProvisioning(
    IConfiguration configuration,
    ILogger<IdentityTopicProvisioning> logger)
{
    private const string CreateTopicsKey = "Messaging:CreateTopics";
    private const string KafkaConnectionName = "kafka";
    private const int PartitionsPerTopic = 6;
    private const short LocalReplicationFactor = 1;

    private static readonly string EventRetention = Milliseconds(TimeSpan.FromDays(30));
    private static readonly string ProfileCompactionLag = Milliseconds(TimeSpan.FromDays(1));

    public async Task ProvisionAsync()
    {
        if (!configuration.GetValue<bool>(CreateTopicsKey))
        {
            return;
        }

        var bootstrapServers = configuration.GetConnectionString(KafkaConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{KafkaConnectionName}' is not configured.");

        var created = await KafkaTopics.EnsureAsync(
            bootstrapServers,
            [
                EventTopic(IdentityTopics.Users),
                EventTopic(IdentityTopics.Erasure),
                ProfileTopic(IdentityTopics.Profiles)
            ]);

        if (created.Count > 0)
        {
            logger.LogInformation(
                "Created {CreatedCount} Kafka topic(s): {Topics}.",
                created.Count,
                string.Join(", ", created));
        }
    }

    private static TopicSpecification EventTopic(string name) =>
        Topic(name, new() { ["cleanup.policy"] = "delete", ["retention.ms"] = EventRetention });

    private static TopicSpecification ProfileTopic(string name) =>
        Topic(name, new()
        {
            ["cleanup.policy"] = "compact",
            ["segment.ms"] = ProfileCompactionLag,
            ["max.compaction.lag.ms"] = ProfileCompactionLag,
            ["delete.retention.ms"] = EventRetention
        });

    private static TopicSpecification Topic(string name, Dictionary<string, string> configs) =>
        new()
        {
            Name = name,
            NumPartitions = PartitionsPerTopic,
            ReplicationFactor = LocalReplicationFactor,
            Configs = configs
        };

    private static string Milliseconds(TimeSpan duration) =>
        ((long)duration.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
}
