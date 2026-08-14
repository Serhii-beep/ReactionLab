using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances;
using ReactionLab.Domain.Substances.Events;
using Shouldly;
using Xunit;
using static ReactionLab.Domain.UnitTests.Substances.MolecularStructureTests;

using BondType = ReactionLab.Domain.Substances.BondType;

namespace ReactionLab.Domain.UnitTests.Substances;

public sealed class SubstanceTests
{
    [Fact]
    public void Create_ProducesAMoleculeAndRaisesTheCreatedEvent()
    {
        var substance = CreateWater().Value;

        substance.Formula.Value.ShouldBe("H2O");
        substance.Name.ShouldBe("Water");
        substance.DomainEvents.OfType<SubstanceCreated>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name) =>
        CreateWater(name).Error.ShouldBe(Substance.NameRequired);

    [Fact]
    public void Create_RejectsOverlongName() =>
        CreateWater(new string('x', Substance.MaximumNameLength + 1))
            .Error.ShouldBe(Substance.NameTooLong);

    [Fact]
    public void DefineStructure_AcceptsAMatchingStructure()
    {
        var substance = CreateWater().Value;

        substance.DefineStructure(Water().Value).IsSuccess.ShouldBeTrue();
        substance.Structure.ShouldNotBeNull();
    }

    [Fact]
    public void DefineStructure_AcceptsAHeavyAtomSkeletonWithNoHydrogens()
    {
        var benzene = Substance.Create(
            ChemicalFormula.Create("C6H6").Value, "Benzene", SubstanceKind.Molecular, true,
            MatterState.Liquid).Value;

        var skeleton = MolecularStructure.Create(
            Enumerable.Range(0, 6).Select(i => Atom("C", i, 0, 0)), []).Value;

        benzene.DefineStructure(skeleton).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void DefineStructure_RejectsPartialHydrogens()
    {
        var substance = CreateWater().Value;

        var oneHydrogen = MolecularStructure.Create(
            [Atom("O", 0, 0, 0), Atom("H", 1, 0, 0)],
            [new Bond(0, 1, BondType.Single)]).Value;

        substance.DefineStructure(oneHydrogen).Error.ShouldBe(Substance.PartialHydrogens);
        substance.Structure.ShouldBeNull();
    }

    [Fact]
    public void DefineStructure_RejectsAStructureMissingHeavyAtoms()
    {
        var caffeine = Substance.Create(
            ChemicalFormula.Create("C8H10N4O2").Value, "Caffeine", SubstanceKind.Molecular, true,
            MatterState.Solid).Value;

        var truncated = MolecularStructure.Create(
            [Atom("C", 0, 0, 0), Atom("C", 1, 0, 0), Atom("C", 2, 0, 0), Atom("C", 3, 0, 0),
            Atom("N", 4, 0, 0), Atom("N", 5, 0, 0)], []).Value;

        caffeine.DefineStructure(truncated).Error.ShouldBe(Substance.StructureCompositionMismatch);
    }

    [Fact]
    public void DefineStructure_RejectsAtomsAbsentFromTheFormula()
    {
        var substance = CreateWater().Value;

        var withSulfur = MolecularStructure.Create(
            [Atom("O", 0, 0, 0), Atom("H", 1, 0, 0), Atom("H", 2, 0, 0), Atom("S", 3, 0, 0)], []).Value;

        substance.DefineStructure(withSulfur).Error.ShouldBe(Substance.StructureCompositionMismatch);
    }

    [Fact]
    public void Describe_TrimsAndDiscardsBlankCommonNames()
    {
        var substance = CreateWater().Value;

        substance.Describe("Oxidane", ["   test1   ", "", "test2"], "Inorganic", "   liquid.");

        substance.IupacName.ShouldBe("Oxidane");
        substance.CommonNames.ShouldBe(["test1", "test2"]);
        substance.Description.ShouldBe("liquid.");
    }

    private static Result<Substance> CreateWater(string? name = "Water") =>
        Substance.Create(
            ChemicalFormula.Create("H2O").Value,
            name,
            SubstanceKind.Molecular,
            isOrganic: false,
            MatterState.Liquid);
}
