using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class DecompositionRuleTests
{
    [Theory]
    [InlineData("CaCO3", "CaO", "decomposition.carbonate")]
    [InlineData("Ca(OH)2", "CaO", "decomposition.hydroxide")]
    [InlineData("Mg(OH)2", "MgO", "decomposition.hydroxide")]
    public void Predict_LeavesTheMetalAsItsOxide(string reactant, string oxide, string rule)
    {
        var prediction = Predictions(reactant).Single();

        prediction.Products[0].ShouldBe(oxide);
        prediction.Rule.ShouldBe(rule);
    }

    [Fact]
    public void Predict_BreaksAGroupOneHydrogencarbonate() =>
        Predictions("NaHCO3").Single().Products.ShouldBe(["Na2CO3", "H2O", "CO2"]);

    [Theory]
    [InlineData("Na2CO3")]
    [InlineData("K2CO3")]
    [InlineData("NaOH")]
    [InlineData("KOH")]
    public void Predict_LeavesTheThermallyStableSaltsAlone(string reactant) =>
        Predictions(reactant).ShouldBeEmpty();

    [Fact]
    public void Predict_CarriesNoTemperature() =>
        Predictions("CaCO3").Single().MinimumKelvin.ShouldBeNull();

    [Theory]
    [InlineData("NaCl")]
    [InlineData("H2O")]
    [InlineData("CH4")]
    public void Predict_RecognizesNothing(string reactant) =>
        Predictions(reactant).ShouldBeEmpty();

    [Theory]
    [InlineData("CaCO3")]
    [InlineData("NaHCO3")]
    [InlineData("Ca(OH)2")]
    [InlineData("Mg(OH)2")]
    public void Predict_ProducesEquationsThatBalance(string reactant)
    {
        var species = Species.Of(reactant);

        foreach (var prediction in Predictions(reactant))
        {
            EquationBalancer.TryBalance(
                [.. prediction.Reactants.Select(index => species[index])],
                Species.Of(string.Join(' ', prediction.Products)),
                out _,
                out var error)
                .ShouldBeTrue($"{reactant} was refused as {error}");
        }
    }

    private static IReadOnlyList<PredictedReaction> Predictions(string reactant) =>
        new ReactionPredictor([new DecompositionRule(TestIons.Table)])
            .Predict(Species.Reagents(reactant));
}
