using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public sealed class Vsepr
{
    private static readonly float Root3Over2 = float.Sqrt(3f) / 2f;
    private static readonly float InverseRoot3 = 1f / float.Sqrt(3f);

    public static MolecularShape ShapeOf(ElectronDomains domains)
    {
        if (domains.BondingDomains <= 1)
        {
            return MolecularShape.Linear;
        }

        return (domains.StericNumber, domains.LonePairs) switch
        {
            (2, 0) => MolecularShape.Linear,
            (3, 0) => MolecularShape.TrigonalPlanar,
            (3, 1) => MolecularShape.Bent,
            (4, 0) => MolecularShape.Tetrahedral,
            (4, 1) => MolecularShape.TrigonalPyramidal,
            (4, 2) => MolecularShape.Bent,
            (5, 0) => MolecularShape.TrigonalBipyramidal,
            (5, 1) => MolecularShape.Seesaw,
            (5, 2) => MolecularShape.TShaped,
            (5, 3) => MolecularShape.Linear,
            (6, 0) => MolecularShape.Octahedral,
            (6, 1) => MolecularShape.SquarePyramidal,
            (6, 2) => MolecularShape.SquarePlanar,
            _ => throw new ArgumentOutOfRangeException(
                nameof(domains), "VSEPR is predictive over two to six electron domains.")
        };
    }

    public static IReadOnlyList<Vector3> Directions(ElectronDomains domains)
    {
        _ = ShapeOf(domains);

        return [.. Arrangement(domains.StericNumber).Skip(domains.LonePairs)];
    }

    private static Vector3[] Arrangement(int stericNumber) => stericNumber switch
    {
        1 => [Vector3.UnitX],
        2 => [Vector3.UnitX, -Vector3.UnitX],
        3 => Equatorial(),
        4 =>
        [
            new(InverseRoot3, InverseRoot3, InverseRoot3),
            new(InverseRoot3, -InverseRoot3, -InverseRoot3),
            new(-InverseRoot3, InverseRoot3, -InverseRoot3),
            new(-InverseRoot3, -InverseRoot3, InverseRoot3)
        ],
        5 => [.. Equatorial(), Vector3.UnitZ, -Vector3.UnitZ],
        6 =>
        [
            Vector3.UnitZ, -Vector3.UnitZ,
            Vector3.UnitX, -Vector3.UnitX,
            Vector3.UnitY, -Vector3.UnitY
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(stericNumber))
    };

    private static Vector3[] Equatorial() =>
        [Vector3.UnitX, new(-0.5f, Root3Over2, 0f), new(-0.5f, -Root3Over2, 0f)];
}
