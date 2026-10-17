using System.Numerics;
using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Symmetry;

namespace ReactionLab.Chemistry.Browser;

internal static class SymmetryDescriber
{
    public static PointGroupEntry Describe(IReadOnlyList<string> symbols, IReadOnlyList<Vector3> positionsAngstroms)
    {
        var group = PointGroupDetector.Detect(symbols, positionsAngstroms);

        return new PointGroupEntry(
            group.Schoenflies,
            Components(group.Center),
            [.. group.RotationAxes.Select(axis => new RotationAxisEntry(Components(axis.Direction), axis.Order))],
            [.. group.MirrorNormals.Select(normal => new MirrorPlaneEntry(Components(normal)))],
            group.HasInversionCenter);
    }

    private static float[] Components(Vector3 vector) => [vector.X, vector.Y, vector.Z];
}
