namespace ReactionLab.Domain.Substances;

public sealed record Bond(int FromAtomIndex, int ToAtomIndex, BondType Type);
