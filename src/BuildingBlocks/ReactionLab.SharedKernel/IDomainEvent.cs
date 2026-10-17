namespace ReactionLab.SharedKernel;

public interface IDomainEvent
{
    Guid EventId { get; }
}
