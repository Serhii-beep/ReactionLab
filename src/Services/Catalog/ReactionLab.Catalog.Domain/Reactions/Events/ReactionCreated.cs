using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Reactions.Events;

public sealed record ReactionCreated(ReactionId ReactionId, string Name) : DomainEvent;
