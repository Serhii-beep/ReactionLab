using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Prediction;

public sealed class SynthesisRuleTests
{
    [Theory]
    [InlineData("Na Cl2", "NaCl")]
    [InlineData("Mg O2", "MgO")]
    [InlineData("Ca O2", "CaO")]
    [InlineData("Al O2", "Al2O3")]
    [InlineData("Zn S", "ZnS")]
    [InlineData("Mg N2", "Mg3N2")]
    public void Predict_CombinesAMetalWithANonMetal(string reactants, string product) =>
        Predictions(reactants).Single().Products.ShouldBe([product]);

    [Fact]
    public void Predict_TakesTheMetalsChargeFromTheActivitySeries() =>
        Predictions("Fe S").Single().Products.ShouldBe(["FeS"]);

    [Theory]
    [InlineData("H2 O2", "H2O")]
    [InlineData("H2 Cl2", "HCl")]
    [InlineData("H2 S", "H2S")]
    public void Predict_CombinesHydrogenWithTheNonMetalsItMeetsDirectly(string reactants, string product) =>
        Predictions(reactants).Single().Products.ShouldBe([product]);

    [Fact]
    public void Predict_DeclinesHydrogenWithNitrogen() =>
        Predictions("H2 N2").ShouldBeEmpty();

    [Theory]
    [InlineData("CaO H2O", "Ca(OH)2")]
    [InlineData("MgO H2O", "Mg(OH)2")]
    [InlineData("Na2O H2O", "NaOH")]
    public void Predict_TurnsABasicOxideIntoItsHydroxide(string reactants, string product) =>
        Predictions(reactants).Single().Products.ShouldBe([product]);

    [Theory]
    [InlineData("NaCl")]
    [InlineData("Cu Ag")]
    [InlineData("CH4 O2")]
    [InlineData("Na")]
    public void Predict_RecognizesNothing(string reactants) =>
        Predictions(reactants).ShouldBeEmpty();

    [Theory]
    [InlineData("Na Cl2")]
    [InlineData("Mg O2")]
    [InlineData("Al O2")]
    [InlineData("Mg N2")]
    [InlineData("H2 O2")]
    [InlineData("CaO H2O")]
    [InlineData("Na2O H2O")]
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
        new ReactionPredictor([new SynthesisRule(TestIons.Series, TestIons.Table)])
            .Predict(Species.Reagents(reactants));
}
