namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SketchRequest(IReadOnlyList<SketchAtomEntry> Atoms, IReadOnlyList<BondEntry> Bonds);
