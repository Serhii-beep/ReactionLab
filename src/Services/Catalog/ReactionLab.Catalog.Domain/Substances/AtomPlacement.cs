using ReactionLab.Catalog.Domain.Elements;

namespace ReactionLab.Catalog.Domain.Substances;

public sealed record AtomPlacement(ElementSymbol Symbol, double X, double Y, double Z);
