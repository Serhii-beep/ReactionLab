using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements.Events;

public sealed record ElementCreated(ElementId ElementId, ElementSymbol Symbol) : DomainEvent;
