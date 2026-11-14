using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace ReactionLab.Messaging.Kafka;

public static class KafkaTopics
{
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(10);

    public static async Task<IReadOnlyList<string>> EnsureAsync(
        AdminClientConfig adminClientConfig,
        IReadOnlyCollection<TopicSpecification> topics)
    {
        using var admin = new AdminClientBuilder(adminClientConfig).Build();

        var partitionCountsByTopic = admin.GetMetadata(MetadataTimeout).Topics
            .ToDictionary(topic => topic.Topic, topic => topic.Partitions.Count);

        foreach (var topic in topics)
        {
            if (partitionCountsByTopic.TryGetValue(topic.Name, out var partitionCount)
                && partitionCount != topic.NumPartitions)
            {
                throw new InvalidOperationException(
                    $"Kafka topic {topic.Name} has {partitionCount} partitions, not {topic.NumPartitions}. "
                    + "Delete it so that it's made again.");
            }
        }

        var created = await CreateMissingAsync(
            admin,
            [.. topics.Where(topic => !partitionCountsByTopic.ContainsKey(topic.Name))]);

        await admin.IncrementalAlterConfigsAsync(topics.ToDictionary(
            topic => new ConfigResource { Type = ResourceType.Topic, Name = topic.Name },
            topic => topic.Configs
                .Select(config => new ConfigEntry
                {
                    Name = config.Key,
                    Value = config.Value,
                    IncrementalOperation = AlterConfigOpType.Set
                })
                .ToList()));

        return created;
    }

    private static async Task<IReadOnlyList<string>> CreateMissingAsync(
        IAdminClient admin,
        IReadOnlyList<TopicSpecification> missing)
    {
        if (missing.Count == 0)
        {
            return [];
        }

        try
        {
            await admin.CreateTopicsAsync(missing);

            return [.. missing.Select(topic => topic.Name)];
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result =>
            result.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            return [.. ex.Results
                .Where(result => result.Error.Code == ErrorCode.NoError)
                .Select(result => result.Topic)];
        }
    }
}
