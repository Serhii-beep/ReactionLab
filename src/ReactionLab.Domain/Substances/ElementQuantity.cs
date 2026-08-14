using ReactionLab.Domain.Elements;

namespace ReactionLab.Domain.Substances;

public readonly record struct ElementQuantity(ElementSymbol Symbol, int Count);
