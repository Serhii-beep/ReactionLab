using System.Numerics;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

internal static class Angles
{
    public static double Between(Vector3 direction, Vector3 other) =>
        double.RadiansToDegrees(Math.Acos(Math.Clamp(
            Vector3.Dot(Vector3.Normalize(direction), Vector3.Normalize(other)), -1f, 1f)));

    public static double At(IReadOnlyList<Vector3> positions, int centre, int first, int second) =>
        Between(positions[first] - positions[centre], positions[second] - positions[centre]);
}
