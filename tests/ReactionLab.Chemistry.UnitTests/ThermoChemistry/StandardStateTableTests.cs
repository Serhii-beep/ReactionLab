using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Thermochemistry;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.ThermoChemistry;

public sealed class StandardStateTableTests
{
    private static readonly StandardStateTable Table = new(
        [
            new("H2O", Phase.Liquid, -285.8m),
            new("H2O", Phase.Gas, -241.8m),
            new("AgCl", Phase.Solid, -127.0m),
            new("Mg", Phase.Solid, 0m)
        ],
        [
            new(new Ion("H", 1), 0m),
            new(new Ion("Na", 1), -240.1m),
            new(new Ion("Ag", 1), 105.6m),
            new(new Ion("Ca", 2), -542.8m),
            new(new Ion("Cl", -1), -167.2m),
            new(new Ion("OH", -1), -230.0m),
            new(new Ion("NO3", -1), -205.0m),
            new(new Ion("SO4", -2), -909.3m)
        ],
        TestIons.Table);

    [Theory]
    [InlineData("H2O", Phase.Liquid, -285.8)]
    [InlineData("H2O", Phase.Gas, -241.8)]
    [InlineData("AgCl", Phase.Solid, -127.0)]
    public void TryFind_ReadsARecordedSpeciesAtItsOwnPhase(string formula, Phase phase, decimal expected)
    {
        Table.TryFind(formula, phase, out var state).ShouldBeTrue();

        state.FormationEnthalpyKjPerMol.ShouldBe(expected);
    }

    [Theory]
    [InlineData("NaCl", -407.3)]
    [InlineData("NaOH", -470.1)]
    [InlineData("AgNO3", -99.4)]
    [InlineData("CaCl2", -877.2)]
    public void TryFind_SumsADissolvedSaltFromItsIons(string formula, decimal expected)
    {
        Table.TryFind(formula, Phase.Aqueous, out var state).ShouldBeTrue();

        state.FormationEnthalpyKjPerMol.ShouldBe(expected, 0.1m);
    }

    [Theory]
    [InlineData("HCl", -167.2)]
    [InlineData("H2SO4", -909.3)]
    public void TryFind_ReadsADissolvedAcidAsItsAnionBecauseTheProtonScaleIsAnchoredAtZero(string formula, decimal expected)
    {
        Table.TryFind(formula, Phase.Aqueous, out var state).ShouldBeTrue();

        state.FormationEnthalpyKjPerMol.ShouldBe(expected, 0.1m);
    }

    [Fact]
    public void TryFind_RefusesASolidWithNoRecordedValue() =>
        Table.TryFind("Zn3(PO4)2", Phase.Solid, out _).ShouldBeFalse();

    [Fact]
    public void TryFind_RefusesADissolvedSaltWhoseIonIsNotRecorded() =>
        Table.TryFind("KCl", Phase.Aqueous, out _).ShouldBeFalse();

    [Fact]
    public void TryFind_DoesNotFallBackToAnotherPhase() =>
        Table.TryFind("Mg", Phase.Aqueous, out _).ShouldBeFalse();
}
