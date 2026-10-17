using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Elements.Events;

public sealed record ElementCreated(ElementId ElementId, ElementSymbol Symbol) : DomainEvent;
