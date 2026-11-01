namespace ReactionLab.Messaging.Outbox;

public sealed class OutboxRelayOptions
{
    public string CloudEventSource { get; set; } = string.Empty;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan TopicRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 500;

    public TimeSpan StateTopicHeartbeatInterval { get; set; } = TimeSpan.FromHours(1);
}
