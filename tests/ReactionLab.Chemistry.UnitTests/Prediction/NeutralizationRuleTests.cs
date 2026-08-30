using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class NeutralizationRuleTests
{
    [Theory]
    [InlineData("NaOH HCl", "NaCl")]
    [InlineData("NaOH H2SO4", "Na2SO4")]
    [InlineData("KOH HNO3", "KNO3")]
    [InlineData("Ca(OH)2 HCl", "CaCl2")]
    [InlineData("Al(OH)3 H2SO4", "Al2(SO4)3")]
    [InlineData("Mg(OH)2 H3PO4", "Mg3(PO4)2")]
    public void Predict_BuildsTheSaltFromTheCationAndTheAnion(string reactants, string salt) =>
        Predictions(reactants).Single().Products.ShouldBe([salt, "H2O"]);

    [Theory]
    [InlineData("NaOH NaOH")]
    [InlineData("HCl HCl")]
    [InlineData("H2O NaOH")]
    [InlineData("CH4 O2")]
    public void Predict_RecognizesNothing(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Theory]
    [InlineData("NaOH HCl")]
    [InlineData("NaOH H2SO4")]
    [InlineData("KOH HNO3")]
    [InlineData("Ca(OH)2 HCl")]
    [InlineData("Al(OH)3 H2SO4")]
    [InlineData("Mg(OH)2 H3PO4")]
    public void Predict_ProducesEquationsThatBalance(string reactants)
    {
        var species = Species.Of(reactants);

        foreach (var prediction in Predictions(reactants))
        {
            EquationBalancer.TryBalance(
                [.. prediction.Reactants.Select(index => species[index])],
                Species.Of(string.Join(' ', prediction.Products)),
                out _,
                out var error)
                .ShouldBeTrue($"{reactants} was refused as {error}");
        }
    }

    private static IReadOnlyList<PredictedReaction> Predictions(string reactants) =>
        new ReactionPredictor([new NeutralizationRule()]).Predict(Species.Reagents(reactants));
}
