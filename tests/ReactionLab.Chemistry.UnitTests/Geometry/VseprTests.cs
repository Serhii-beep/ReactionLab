using System.Numerics;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

public sealed class VseprTests
{
    [Theory]
    [InlineData("CO2", 2, 0, MolecularShape.Linear)]
    [InlineData("BF3", 3, 0, MolecularShape.TrigonalPlanar)]
    [InlineData("SO2", 2, 1, MolecularShape.Bent)]
    [InlineData("CH4", 4, 0, MolecularShape.Tetrahedral)]
    [InlineData("NH3", 3, 1, MolecularShape.TrigonalPyramidal)]
    [InlineData("H2O", 2, 2, MolecularShape.Bent)]
    [InlineData("PCl5", 5, 0, MolecularShape.TrigonalBipyramidal)]
    [InlineData("SF4", 4, 1, MolecularShape.Seesaw)]
    [InlineData("ClF3", 3, 2, MolecularShape.TShaped)]
    [InlineData("XeF2", 2, 3, MolecularShape.Linear)]
    [InlineData("SF6", 6, 0, MolecularShape.Octahedral)]
    [InlineData("BrF5", 5, 1, MolecularShape.SquarePyramidal)]
    [InlineData("XeF4", 4, 2, MolecularShape.SquarePlanar)]
    [InlineData("HCl", 1, 3, MolecularShape.Linear)]
    public void ShapeOf_MatchesTheGeometry(
        string species, int bonding, int lonePairs, MolecularShape shape) =>
        Vsepr.ShapeOf(new ElectronDomains(bonding, lonePairs)).ShouldBe(shape, species);

    [Theory]
    [InlineData("CO2 linear", 2, 0, "180")]
    [InlineData("BF3 trigonal planar", 3, 0, "120")]
    [InlineData("CH4 tetrahedral", 4, 0, "109.5")]
    [InlineData("PCl5 trigonal bipyramidal", 5, 0, "90 120 180")]
    [InlineData("SF4 seesaw", 4, 1, "90 120 180")]
    [InlineData("ClF3 T-shaped", 3, 2, "90 180")]
    [InlineData("SF6 octahedral", 6, 0, "90 180")]
    [InlineData("XeF4 square planar", 4, 2, "90 180")]
    public void Directions_SeparateTheBondsByTheIdealAngles(
        string species, int bonding, int lonePairs, string angles)
    {
        var directions = Vsepr.Directions(new ElectronDomains(bonding, lonePairs));

        directions.Count.ShouldBe(bonding, species);
        directions.ShouldAllBe(direction => Math.Abs(direction.Length() - 1f) < 1e-6f);
        Angles(directions).ShouldBe(angles, species);
    }

    [Theory]
    [InlineData(0, "90 120 180")]
    [InlineData(1, "90 120 180")]
    [InlineData(2, "90 180")]
    [InlineData(3, "180")]
    public void Directions_SeatLonePairsEquatoriallyInATrigonalBipyramid(
        int lonePairs, string angles) =>
        Angles(Vsepr.Directions(new ElectronDomains(5 - lonePairs, lonePairs))).ShouldBe(angles);

    [Fact]
    public void Directions_SeatLonePairsOppositeEachOtherInAnOctahedron()
    {
        var square = Vsepr.Directions(new ElectronDomains(4, 2));

        square.ShouldAllBe(direction => Math.Abs(direction.Z) < 1e-6f);
        Angles(square).ShouldBe("90 180");
    }

    [Fact]
    public void ShapeOf_RefusesMoreDomainsThanVseprPredicts() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = Vsepr.ShapeOf(new ElectronDomains(7, 0)));

    private static string Angles(IReadOnlyList<Vector3> directions)
    {
        var angles = new List<double>();

        for (var first = 0; first < directions.Count; first++)
        {
            for (var second = first + 1; second < directions.Count; second++)
            {
                angles.Add(Math.Round(AngleDegrees(directions[first], directions[second]), 1));
            }
        }

        return string.Join(' ', angles.Distinct().Order());
    }

    private static double AngleDegrees(Vector3 direction, Vector3 other) =>
        double.RadiansToDegrees(Math.Acos(Math.Clamp(
            Vector3.Dot(Vector3.Normalize(direction), Vector3.Normalize(other)), -1f, 1f)));
}
