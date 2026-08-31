using ReactionLab.Chemistry.Thermochemistry;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.ThermoChemistry;

public sealed class PhaseResolutionTests
{
    private static readonly PhaseResolution Resolution = new(TestIons.Table, TestIons.StandardStates);

    [Theory]
    [InlineData("precipitation.halides", "AgNO3", Phase.Aqueous)]
    [InlineData("precipitation.halides", "AgCl", Phase.Solid)]
    [InlineData("precipitation.halides", "NaNO3", Phase.Aqueous)]
    [InlineData("precipitation.sulfates", "BaSO4", Phase.Solid)]
    [InlineData("neutralization.saltAndWater", "HCl", Phase.Aqueous)]
    [InlineData("neutralization.saltAndWater", "NaOH", Phase.Aqueous)]
    [InlineData("neutralization.saltAndWater", "NaCl", Phase.Aqueous)]
    [InlineData("neutralization.saltAndWater", "H2O", Phase.Liquid)]
    [InlineData("singleReplacement.metalAndAcid", "Zn", Phase.Solid)]
    [InlineData("singleReplacement.metalAndAcid", "HCl", Phase.Aqueous)]
    [InlineData("singleReplacement.metalAndAcid", "H2", Phase.Gas)]
    [InlineData("singleReplacement.metalAndColdWater", "H2O", Phase.Liquid)]
    [InlineData("singleReplacement.metalAndSteam", "H2O", Phase.Gas)]
    [InlineData("singleReplacement.metalAndSteam", "MgO", Phase.Solid)]
    [InlineData("combustion.complete", "CH4", Phase.Gas)]
    [InlineData("combustion.complete", "H2O", Phase.Liquid)]
    [InlineData("combustion.complete", "CO2", Phase.Gas)]
    [InlineData("synthesis.metalAndNonMetal", "MgO", Phase.Solid)]
    [InlineData("synthesis.oxideAndWater", "Ca(OH)2", Phase.Solid)]
    [InlineData("decomposition.carbonate", "CaCO3", Phase.Solid)]
    [InlineData("decomposition.hydrogencarbonate", "H2O", Phase.Gas)]
    public void TryPhaseOf_ReproducesTheCuratedCatalogsOwnStates(string rule, string formula, Phase expected)
    {
        Resolution.TryPhaseOf(rule, formula, out var phase).ShouldBeTrue();

        phase.ShouldBe(expected);
    }

    [Fact]
    public void TryPhaseOf_RefusesASpeciesWithNoRecordedState() =>
        Resolution.TryPhaseOf("synthesis.metalAndNonMetal", "K3N", out _).ShouldBeFalse();

    [Fact]
    public void TryResolve_AssignsEveryParticipant()
    {
        Resolution.TryResolve(
            "singleReplacement.metalAndColdWater", ["Na", "H2O"], ["NaOH", "H2"], out var assignment)
            .ShouldBeTrue();

        assignment.Reactants.ShouldBe([Phase.Solid, Phase.Liquid]);
        assignment.Products.ShouldBe([Phase.Aqueous, Phase.Gas]);
    }

    [Fact]
    public void TryResolve_RefusesWhenOneParticipantCannotBePlaced() =>
        Resolution.TryResolve("synthesis.metalAndNonMetal", ["K", "N2"], ["K3N"], out _).ShouldBeFalse();
}
