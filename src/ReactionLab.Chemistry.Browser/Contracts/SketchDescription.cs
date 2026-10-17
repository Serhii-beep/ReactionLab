namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SketchDescription(
    IReadOnlyList<SketchAtomDescription> Atoms,
    string? Hill,
    int Charge,
    decimal? GramsPerMole,
    IReadOnlyList<float>? PositionsAngstroms,
    IReadOnlyList<int>? OpenValenceOwners,
    IReadOnlyList<float>? OpenValencePositionsAngstroms,
    string? GeometryError,
    PointGroupEntry? PointGroup);
