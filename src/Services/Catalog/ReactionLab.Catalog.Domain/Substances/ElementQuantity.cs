using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Domain.Substances;

public readonly record struct ElementQuantity(ElementSymbol Symbol, int Count);
