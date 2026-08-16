using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions.Events;

public sealed record ReactionCreated(ReactionId ReactionId, string Name) : DomainEvent;
