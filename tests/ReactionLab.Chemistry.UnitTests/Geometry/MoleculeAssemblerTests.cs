using System.Numerics;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

public sealed class MoleculeAssemblerTests
{
    private static readonly Dictionary<string, AtomicGeometry> Table = new(StringComparer.Ordinal)
    {
        ["H"] = new(1, 31),
        ["C"] = new(4, 76, 67, 60),
        ["N"] = new(5, 71, 60, 54),
        ["O"] = new(6, 66, 57, 53),
        ["P"] = new(5, 107, 102, 94),
        ["S"] = new(6, 105, 94, 95),
        ["Cl"] = new(7, 102, 99)
    };

    [Fact]
    public void TryAssemble_SeparatesTwoAtomsByTheSumOfTheirRadii()
    {
        var placed = Assemble([Atom("H"), Atom("Cl")], [new AtomBond(0, 1, 1)]);

        Vector3.Distance(placed[0], placed[1]).ShouldBe(1.33f, 0.001f);
    }

    [Fact]
    public void TryAssemble_OpensWaterToTheIdealTetrahedralAngle()
    {
        var placed = Assemble([Atom("O"), Atom("H"), Atom("H")], [new(0, 1, 1), new(0, 2, 1)]);

        Angles.At(placed, 0, 1, 2).ShouldBe(109.5d, 0.1d);
        Vector3.Distance(placed[0], placed[1]).ShouldBe(0.97f, 0.001f);
    }

    [Fact]
    public void TryAssemble_MakesMethaneTetrahedral()
    {
        var placed = Assemble(
            [Atom("C"), Atom("H"), Atom("H"), Atom("H"), Atom("H")],
            [new(0, 1, 1), new(0, 2, 1), new(0, 3, 1), new(0, 4, 1)]);

        for (var first = 1; first <= 4; first++)
        {
            for (var second = first + 1; second <= 4; second++)
            {
                Angles.At(placed, 0, first, second).ShouldBe(109.5d, 0.1d);
            }
        }
    }

    [Fact]
    public void TryAssemble_MakesCarbonDioxideLinearOnItsShorterDoubleBonds()
    {
        var placed = Assemble([Atom("C"), Atom("O"), Atom("O")], [new(0, 1, 2), new(0, 2, 2)]);

        Angles.At(placed, 0, 1, 2).ShouldBe(180d, 0.1d);
        Vector3.Distance(placed[0], placed[1]).ShouldBe(1.24f, 0.001f);
    }

    [Fact]
    public void TryAssemble_ChainsShapesThroughABond()
    {
        var placed = Assemble(
            [Atom("O"), Atom("O"), Atom("H"), Atom("H")],
            [new(0, 1, 1), new(0, 2, 1), new(1, 3, 1)]);

        Angles.At(placed, 0, 1, 2).ShouldBe(109.5d, 0.1d);
        Angles.At(placed, 1, 0, 3).ShouldBe(109.5d, 0.1d);
        Vector3.Distance(placed[0], placed[1]).ShouldBe(1.32f, 0.001f);
    }

    [Fact]
    public void TryAssemble_ReadsTheFormalCharge()
    {
        Refuse([Atom("C"), Atom("O")], [new AtomBond(0, 1, 3)]).ShouldBe(AssemblyError.ShapeUnavailable);

        Assemble([new SkeletalAtom("C", -1), new SkeletalAtom("O", 1)], [new AtomBond(0, 1, 3)])
            .Count.ShouldBe(2);
    }

    [Fact]
    public void TryAssemble_RefusesARing() =>
        Refuse(
            [Atom("C"), Atom("C"), Atom("C")],
            [new(0, 1, 1), new(1, 2, 1), new(2, 0, 1)]).ShouldBe(AssemblyError.Cyclic);

    [Fact]
    public void TryAssemble_RefusesTwoMoleculesInOneGraph() =>
        Refuse(
            [Atom("H"), Atom("H"), Atom("O"), Atom("O")],
            [new(0, 1, 1), new(2, 3, 2)]).ShouldBe(AssemblyError.Disconnected);

    [Fact]
    public void TryAssemble_RefusesAnElementItHasNoDataFor() =>
        Refuse([Atom("C"), Atom("Xe")], [new(0, 1, 1)]).ShouldBe(AssemblyError.UnknownElement);

    [Fact]
    public void TryAssemble_RefusesARadical() =>
        Refuse(
            [Atom("C"), Atom("H"), Atom("H"), Atom("H")],
            [new(0, 1, 1), new(0, 2, 1), new(0, 3, 1)]).ShouldBe(AssemblyError.ShapeUnavailable);

    [Fact]
    public void TryAssemble_RefusesAMoleculeWithNoAtoms() =>
        Refuse([], []).ShouldBe(AssemblyError.NoAtoms);

    private static SkeletalAtom Atom(string symbol) => new(symbol);

    private static IReadOnlyList<Vector3> Assemble(
        IReadOnlyList<SkeletalAtom> atoms, IReadOnlyList<AtomBond> bonds)
    {
        MoleculeAssembler.TryAssemble(atoms, bonds, Table, out var placed, out var error)
            .ShouldBeTrue($"assembly was refused as {error}");

        return placed;
    }

    private static AssemblyError Refuse(
        IReadOnlyList<SkeletalAtom> atoms, IReadOnlyList<AtomBond> bonds)
    {
        MoleculeAssembler.TryAssemble(atoms, bonds, Table, out _, out var error).ShouldBeFalse();

        return error;
    }
}
