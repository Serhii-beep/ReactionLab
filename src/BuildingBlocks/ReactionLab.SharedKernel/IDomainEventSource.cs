namespace ReactionLab.SharedKernel;

public interface IDomainEventSource
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}
