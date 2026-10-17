using System.Numerics;

namespace ReactionLab.Chemistry.Symmetry;

public readonly record struct RotationAxis(Vector3 Direction, int Order)
{
    public const int InfiniteOrder = 0;
}
