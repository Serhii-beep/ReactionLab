namespace ReactionLab.Messaging.Consuming;

public interface IConsumedEventEffects
{
    Task ApplyAsync(ConsumedEvent consumed, CancellationToken cancellationToken);
}
