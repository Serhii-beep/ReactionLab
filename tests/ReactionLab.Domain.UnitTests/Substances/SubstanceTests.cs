using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
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
        substance.Content(SupportedLocale.English).Name.ShouldBe("Water");
        substance.DomainEvents.OfType<SubstanceCreated>().Count().ShouldBe(1);
    }

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
            ChemicalFormula.Create("C6H6").Value,
            SubstanceContent.Create("Benzene").Value,
            SubstanceKind.Molecular, true,
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
            ChemicalFormula.Create("C8H10N4O2").Value,
            SubstanceContent.Create("Caffeine").Value,
            SubstanceKind.Molecular, true,
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
    public void Translate_AddsALocaleAndFallsBackPerValue()
    {
        var substance = CreateWater().Value;

        substance.Translate(
            SupportedLocale.Ukrainian,
            SubstanceContent.Create("Translated", description: "Translated").Value);

        var translated = substance.Content(SupportedLocale.Ukrainian);

        translated.Name.ShouldBe("Translated");
        translated.Description.ShouldBe("Translated");
        translated.IupacName.ShouldBe("Oxidane");
    }

    [Fact]
    public void Classify_StoresACategoryKey()
    {
        var substance = CreateWater().Value;

        substance.Classify("   Test   ");

        substance.Category.ShouldBe("Test");
    }

    private static Result<Substance> CreateWater(string? name = "Water") =>
        Substance.Create(
            ChemicalFormula.Create("H2O").Value,
            SubstanceContent.Create("Water", iupacName: "Oxidane").Value,
            SubstanceKind.Molecular,
            isOrganic: false,
            MatterState.Liquid);
}
