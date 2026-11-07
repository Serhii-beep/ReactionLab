namespace ReactionLab.Messaging.Consuming;

public sealed class EventConsumerOptions
{
    public IReadOnlyCollection<string> Topics { get; set; } = [];

    public IReadOnlyCollection<string> EventTypes { get; set; } = [];

    public IReadOnlyCollection<string> ErasureEventTypes { get; set; } = [];

    public int Retries { get; set; } = 3;

    public TimeSpan FirstRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan AttemptBudget { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan HoldDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(40);
}
