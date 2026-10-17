using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class PrecipitationRuleTests
{
    [Theory]
    [InlineData("AgNO3 NaCl", "AgCl", "NaNO3")]
    [InlineData("Pb(NO3)2 KI", "PbI2", "KNO3")]
    [InlineData("BaCl2 Na2SO4", "BaSO4", "NaCl")]
    [InlineData("CuSO4 NaOH", "Cu(OH)2", "Na2SO4")]
    [InlineData("Na2CO3 CaCl2", "NaCl", "CaCO3")]
    public void Predict_SwapsPartnersWhenOneNewPairingWillNotDissolve(
        string reactants, string first, string second) =>
        Predictions(reactants).Single().Products.ShouldBe([first, second]);

    [Fact]
    public void Predict_NamesTheRuleThatMadeThePrecipitate() =>
        Predictions("BaCl2 Na2SO4").Single().Rule.ShouldBe("precipitation.sulfates");

    [Theory]
    [InlineData("Li3N CuSO4")]
    [InlineData("Li3N BaCl2")]
    [InlineData("Li2O CuSO4")]
    [InlineData("K2O MgCl2")]
    public void Predict_WillNotDrawAnIonOutOfASaltThatWaterDestroys(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Theory]
    [InlineData("NaCl KNO3")]
    [InlineData("NaNO3 KCl")]
    [InlineData("AgCl NaNO3")]
    [InlineData("AgNO3")]
    [InlineData("CH4 O2")]
    public void Predict_RecognizesNothing(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Theory]
    [InlineData("AgNO3 NaCl")]
    [InlineData("Pb(NO3)2 KI")]
    [InlineData("BaCl2 Na2SO4")]
    [InlineData("CuSO4 NaOH")]
    [InlineData("Na2CO3 CaCl2")]
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
                .ShouldBeTrue($"{reactants} was refused as {error}");
        }
    }

    private static IReadOnlyList<PredictedReaction> Predictions(string reactants) =>
        new ReactionPredictor([new PrecipitationRule(TestIons.Table)])
            .Predict(Species.Reagents(reactants));
}
