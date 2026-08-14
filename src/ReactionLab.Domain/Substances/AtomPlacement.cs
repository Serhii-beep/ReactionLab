using ReactionLab.Domain.Elements;

namespace ReactionLab.Domain.Substances;

public sealed record AtomPlacement(ElementSymbol Symbol, double X, double Y, double Z);
