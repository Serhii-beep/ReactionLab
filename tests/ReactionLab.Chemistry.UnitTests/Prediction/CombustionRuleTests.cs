using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class CombustionRuleTests
{
    private static readonly int[] FuelAndOxygen = [1, 2];

    [Theory]
    [InlineData("C3H8 O2", "CO2+H2O", "CO+H2O", "C+H2O")]
    [InlineData("C2H6O O2", "CO2+H2O", "CO+H2O", "C+H2O")]
    [InlineData("C O2", "CO2", "CO", null)]
    [InlineData("CO O2", "CO2", null, null)]
    [InlineData("C6H12O6 O2", "CO2+H2O", "CO+H2O", null)]
    public void Predict_OffersEachOutcomeThatWouldConsumeOxygen(
        string reactants, string? complete, string? incomplete, string? sooting)
    {
        var products = Predictions(reactants).ToDictionary(
            prediction => prediction.Rule,
            prediction => string.Join('+', prediction.Products),
            StringComparer.Ordinal);

        Product(products, "combustion.complete").ShouldBe(complete);
        Product(products, "combustion.incomplete").ShouldBe(incomplete);
        Product(products, "combustion.sooting").ShouldBe(sooting);
    }

    [Theory]
    [InlineData("CO2 O2")]
    [InlineData("C3H8")]
    [InlineData("O3 C3H8")]
    [InlineData("H2 O2")]
    [InlineData("NaCl O2")]
    public void Predict_RecognizesNothing(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Fact]
    public void Predict_RanksCompleteCombustionAboveTheAlternatives() =>
        Predictions("C3H8 O2").Select(prediction => prediction.Rule).ShouldBe(
            ["combustion.complete", "combustion.incomplete", "combustion.sooting"]);

    [Fact]
    public void Predict_NamesOnlyTheReactantsItConsumed() =>
        Predictions("NaCl C3H8 O2").ShouldAllBe(prediction => prediction.Reactants.SequenceEqual(FuelAndOxygen));

    [Theory]
    [InlineData("C3H8 O2")]
    [InlineData("CH4 O2")]
    [InlineData("C2H2 O2")]
    [InlineData("C2H6O O2")]
    [InlineData("C6H12O6 O2")]
    [InlineData("C O2")]
    [InlineData("CO O2")]
    public void Predict_ProducesEquationsThatBalance(string reactants)
    {
        var species = Species.Of(reactants);

        foreach (var prediction in Predictions(reactants))
        {
            EquationBalancer.TryBalance(
                [.. prediction.Reactants.Select(i => species[i])],
                Species.Of(string.Join(' ', prediction.Products)),
                out _,
                out var error)
                .ShouldBeTrue($"{reactants} by {prediction.Rule} was refused as {error}");
        }
    }

    private static IReadOnlyList<PredictedReaction> Predictions(string reactants) =>
        new ReactionPredictor([new CombustionRule()]).Predict(Species.Reagents(reactants));

    private static string? Product(Dictionary<string, string> products, string rule) =>
        products.TryGetValue(rule, out var formula) ? formula : null;
}
