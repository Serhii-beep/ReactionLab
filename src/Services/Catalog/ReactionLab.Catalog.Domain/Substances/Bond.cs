namespace ReactionLab.Catalog.Domain.Substances;

public sealed record Bond(int FromAtomIndex, int ToAtomIndex, BondType Type);
