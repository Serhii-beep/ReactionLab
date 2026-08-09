namespace ReactionLab.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
}
