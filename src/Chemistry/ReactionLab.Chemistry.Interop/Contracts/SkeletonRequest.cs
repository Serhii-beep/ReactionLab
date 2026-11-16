namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record SkeletonRequest(IReadOnlyList<AtomEntry> Atoms, IReadOnlyList<BondEntry> Bonds);
