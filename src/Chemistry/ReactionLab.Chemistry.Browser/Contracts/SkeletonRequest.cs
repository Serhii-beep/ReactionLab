namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SkeletonRequest(IReadOnlyList<AtomEntry> Atoms, IReadOnlyList<BondEntry> Bonds);
