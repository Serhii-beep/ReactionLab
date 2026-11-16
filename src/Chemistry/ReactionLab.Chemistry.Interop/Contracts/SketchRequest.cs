namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record SketchRequest(IReadOnlyList<SketchAtomEntry> Atoms, IReadOnlyList<BondEntry> Bonds);
