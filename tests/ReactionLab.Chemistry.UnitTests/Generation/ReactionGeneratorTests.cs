using ReactionLab.Chemistry.Generation;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.UnitTests.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Generation;

public sealed class ReactionGeneratorTests
{
    [Fact]
    public void From_FindsReactionsOfOneReactantAndOfTwo()
    {
        var rules = Generated("CaCO3 Na Cl2 O2 CH4");

        rules.ShouldContain(reaction => reaction.Signature == "CaCO3 -> CO2 + CaO");
        rules.ShouldContain(reaction => reaction.Signature == "Cl2 + Na -> NaCl");
        rules.ShouldContain(reaction => reaction.Signature == "CH4 + O2 -> CO2 + H2O");
    }

    [Fact]
    public void From_GivesEveryCandidateWholePositiveCoefficients() =>
        Generated("CaCO3 Na Cl2 O2 CH4 HCl NaOH Zn CuSO4").ShouldAllBe(reaction =>
            reaction.Reactants.All(participant => participant.Coefficient > 0)
            && reaction.Products.All(participant => participant.Coefficient > 0));

    [Fact]
    public void From_KeepsOneCandidatePerEquation() =>
        Generated("Na Cl2 O2 CH4 HCl NaOH")
            .Select(reaction => reaction.Signature)
            .ShouldBeUnique();

    [Fact]
    public void From_RanksTheMostConfidentFirst()
    {
        var confidences = Generated("CH4 O2 HCl NaOH").Select(reaction => reaction.Confidence).ToList();

        confidences.ShouldBe(confidences.OrderByDescending(value => value));
    }

    [Fact]
    public void From_GivesTheSameCatalogueTwice()
    {
        var once = Generated("Na Cl2 O2 CH4 HCl NaOH CaCO3");
        var again = Generated("Na Cl2 O2 CH4 HCl NaOH CaCO3");

        once.Select(reaction => reaction.Signature)
            .ShouldBe(again.Select(reaction => reaction.Signature));
    }

    [Fact]
    public void From_DropsWhatTheBalancerRefuses()
    {
        var generator = new ReactionGenerator(new ReactionPredictor([new ImpossibleRule()]));

        generator.From(Species.Reagents("H2 O2")).ShouldBeEmpty();
    }

    [Fact]
    public void From_FindsNothingInAnEmptyCatalogue() =>
        new ReactionGenerator(new ReactionPredictor([new CombustionRule()])).From([]).ShouldBeEmpty();

    private static IReadOnlyList<GeneratedReaction> Generated(string substances) =>
        new ReactionGenerator(new ReactionPredictor(
        [
            new CombustionRule(),
            new NeutralizationRule(TestIons.Table),
            new PrecipitationRule(TestIons.Table),
            new SingleReplacementRule(TestIons.Series, TestIons.Table),
            new SynthesisRule(TestIons.Series, TestIons.Table),
            new DecompositionRule(TestIons.Table)
        ])).From(Species.Reagents(substances));

    private sealed class ImpossibleRule : IReactionRule
    {
        public string Name => "test.impossible";

        public IEnumerable<PredictedReaction> Predict(IReadOnlyList<Reagent> reactants) =>
            [new PredictedReaction([0], ["Au"], "test.impossible", 0.99m)];
    }
}
