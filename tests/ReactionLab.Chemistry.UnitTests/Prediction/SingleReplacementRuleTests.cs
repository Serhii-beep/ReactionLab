using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class SingleReplacementRuleTests
{
    [Theory]
    [InlineData("Zn HCl", "ZnCl2", "H2")]
    [InlineData("Mg HCl", "MgCl2", "H2")]
    [InlineData("Zn H2SO4", "ZnSO4", "H2")]
    public void Predict_TakesHydrogenOutOfAnAcid(string reactants, string salt, string gas) =>
        Predictions(reactants).Single().Products.ShouldBe([salt, gas]);

    [Theory]
    [InlineData("Fe CuSO4", "FeSO4", "Cu")]
    [InlineData("Zn CuSO4", "ZnSO4", "Cu")]
    [InlineData("Al CuSO4", "Al2(SO4)3", "Cu")]
    [InlineData("Cu AgNO3", "Cu(NO3)2", "Ag")]
    public void Predict_TakesAMetalOutOfItsSalt(string reactants, string salt, string metal) =>
        Predictions(reactants).Single().Products.ShouldBe([salt, metal]);

    [Theory]
    [InlineData("Na H2O", "NaOH", "singleReplacement.metalAndColdWater")]
    [InlineData("Ca H2O", "Ca(OH)2", "singleReplacement.metalAndColdWater")]
    [InlineData("Mg H2O", "MgO", "singleReplacement.metalAndSteam")]
    public void Predict_LeavesAHydroxideInColdWaterAndAnOxideInSteam(
        string reactants, string product, string rule)
    {
        var prediction = Predictions(reactants).Single();

        prediction.Products.ShouldBe([product, "H2"]);
        prediction.Rule.ShouldBe(rule);
    }

    [Fact]
    public void Predict_ReportsTheTemperatureSteamNeeds()
    {
        Predictions("Mg H2O").Single().MinimumKelvin.ShouldBe(373.15m);

        Predictions("Na H2O").Single().MinimumKelvin.ShouldBeNull();
    }

    [Theory]
    [InlineData("Cu HCl")]
    [InlineData("Ag HCl")]
    [InlineData("Cu FeSO4")]
    [InlineData("Zn NaCl")]
    [InlineData("Cu H2O")]
    [InlineData("Zn")]
    [InlineData("CH4 O2")]
    public void Predict_RecognizesNothing(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Theory]
    [InlineData("Zn HCl")]
    [InlineData("Mg HCl")]
    [InlineData("Na H2O")]
    [InlineData("Mg H2O")]
    [InlineData("Fe CuSO4")]
    [InlineData("Al CuSO4")]
    [InlineData("Cu AgNO3")]
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
        new ReactionPredictor([new SingleReplacementRule(TestIons.Series, TestIons.Table)])
            .Predict(Species.Reagents(reactants));
}
