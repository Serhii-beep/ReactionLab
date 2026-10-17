using System.Numerics;

namespace ReactionLab.Chemistry.Browser;

internal sealed record SketchGeometry(
    IReadOnlyList<Vector3>? PositionsAngstroms,
    IReadOnlyList<int> OpenValenceOwners,
    IReadOnlyList<Vector3>? OpenValencePositionsAngstroms,
    int Fragments,
    string? Error)
{
    public static SketchGeometry Empty { get; } = new([], [], [], 0, null);

    public static SketchGeometry Failed(string error) => new(null, [], null, 0, error);
}
