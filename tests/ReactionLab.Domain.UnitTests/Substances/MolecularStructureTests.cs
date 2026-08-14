using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Substances;

public sealed class MolecularStructureTests
{
    [Fact]
    public void Create_AcceptsAValidStructure()
    {
        var structure = Water().Value;

        structure.Atoms.Count.ShouldBe(3);
        structure.Bonds.Count.ShouldBe(2);
        structure.HasExplicitHydrogens.ShouldBeTrue();
    }

    [Fact]
    public void Create_RejectsAnEmptyAtomList() =>
        MolecularStructure.Create([], []).Error.ShouldBe(MolecularStructure.NoAtoms);

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 0)]
    public void Create_RejectsBondIndicesOutOfRange(int from, int to)
    {
        var result = MolecularStructure.Create(
            [Atom("O", 0, 0, 0), Atom("H", 1, 0, 0)],
            [new Bond(from, to, BondType.Single)]);

        result.Error.ShouldBe(MolecularStructure.BondIndexOutOfRange);
    }

    [Fact]
    public void Create_RejectsASelfBond()
    {
        var result = MolecularStructure.Create(
            [Atom("O", 0, 0, 0)],
            [new Bond(0, 0, BondType.Single)]);

        result.Error.ShouldBe(MolecularStructure.SelfBond);
    }

    [Fact]
    public void Create_RejectsTheSamePairBondedTwice()
    {
        var result = MolecularStructure.Create(
            [Atom("O", 0, 0, 0), Atom("H", 1, 0, 0)],
            [new Bond(0, 1, BondType.Single), new Bond(1, 0, BondType.Double)]);

        result.Error.ShouldBe(MolecularStructure.DuplicateBond);
    }

    [Fact]
    public void HasExplicitHydrogens_IsFalseForAHeavyAtomSkeleton()
    {
        var benzene = MolecularStructure.Create(
            Enumerable.Range(0, 6).Select(i => Atom("C", i, 0, 0)), []).Value;

        benzene.HasExplicitHydrogens.ShouldBeFalse();
    }

    [Fact]
    public void Composition_CountsAtomsByElement()
    {
        var composition = Water().Value.Composition();

        composition[ElementSymbol.Create("H").Value].ShouldBe(2);
        composition[ElementSymbol.Create("O").Value].ShouldBe(1);
    }

    internal static AtomPlacement Atom(string symbol, double x, double y, double z) =>
        new(ElementSymbol.Create(symbol).Value, x, y, z);

    internal static Result<MolecularStructure> Water() =>
        MolecularStructure.Create(
            [Atom("O", 0, 0, 0), Atom("H", 0.757, 0.856, 0), Atom("H", -0.757, 0.586, 0)], [new Bond(0, 1, BondType.Single), new Bond(0, 2, BondType.Single)]);
}
