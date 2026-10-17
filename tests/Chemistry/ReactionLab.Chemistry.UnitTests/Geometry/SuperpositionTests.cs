using System.Numerics;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

public sealed class SuperpositionTests
{
    private static readonly Vector3[] Tetrahedron =
    [
        new(1f, 1f, 1f), new(1f, -1f, -1f), new(-1f, 1f, -1f), new(-1f, -1f, 1f)
    ];

    [Fact]
    public void TryRootMeanSquareDeviation_IsZeroForIdenticalShapes()
    {
        Superposition.TryRootMeanSquareDeviation(Tetrahedron, Tetrahedron, out var deviation)
            .ShouldBeTrue();

        deviation.ShouldBe(0d, 1e-5d);
    }

    [Fact]
    public void TryRootMeanSquareDeviation_IgnoresTranslation()
    {
        var moved = Tetrahedron.Select(point => point + new Vector3(10f, -4f, 7f)).ToArray();

        Superposition.TryRootMeanSquareDeviation(Tetrahedron, moved, out var deviation).ShouldBeTrue();

        deviation.ShouldBe(0d, 1e-5d);
    }

    [Fact]
    public void TryRootMeanSquareDeviation_IgnoresRotation()
    {
        var turned = Tetrahedron
            .Select(point => Vector3.Transform(
                point, Matrix4x4.CreateFromYawPitchRoll(0.7f, -1.2f, 0.35f)))
            .ToArray();

        Superposition.TryRootMeanSquareDeviation(Tetrahedron, turned, out var deviation).ShouldBeTrue();

        deviation.ShouldBe(0d, 1e-4d);
    }

    [Fact]
    public void TryRootMeanSquareDeviation_RefusesToAlignAMirrorImage()
    {
        var mirrored = Tetrahedron.Select(point => point with { Z = -point.Z }).ToArray();

        Superposition.TryRootMeanSquareDeviation(Tetrahedron, mirrored, out var deviation).ShouldBeTrue();

        deviation.ShouldBeGreaterThan(1d);
    }

    [Fact]
    public void TryRootMeanSquareDeviation_ReportsTheResidualWhenShapesDiffer()
    {
        Vector3[] stretched = [new(0f, 0f, 0f), new(2f, 0f, 0f)];
        Vector3[] shorter = [new(0f, 0f, 0f), new(1f, 0f, 0f)];

        Superposition.TryRootMeanSquareDeviation(stretched, shorter, out var deviation).ShouldBeTrue();

        deviation.ShouldBe(0.5d, 1e-5d);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TryRootMeanSquareDeviation_RefusesMismatchedOrEmptyInput(int count) =>
        Superposition.TryRootMeanSquareDeviation(
            Tetrahedron.Take(count).ToArray(), [], out _).ShouldBeFalse();

    [Fact]
    public void TryBestRootMeanSquareDeviation_AllowsEquivalentAtomsToTradePlaces()
    {
        Vector3[] swapped = [Tetrahedron[0], Tetrahedron[2], Tetrahedron[1], Tetrahedron[3]];

        Superposition.TryRootMeanSquareDeviation(Tetrahedron, swapped, out var indexed).ShouldBeTrue();
        indexed.ShouldBeGreaterThan(0.5d);

        Superposition.TryBestRootMeanSquareDeviation(
            Tetrahedron, swapped, [[1, 2]], out var best).ShouldBeTrue();
        best.ShouldBe(0d, 1e-5d);
    }
}
