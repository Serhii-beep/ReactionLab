namespace ReactionLab.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
