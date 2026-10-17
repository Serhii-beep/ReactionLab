using System.Numerics;

namespace ReactionLab.Chemistry.Symmetry;

public sealed record PointGroup(
    string Schoenflies,
    Vector3 Center,
    IReadOnlyList<RotationAxis> RotationAxes,
    IReadOnlyList<Vector3> MirrorNormals,
    bool HasInversionCenter);
