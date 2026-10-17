using ReactionLab.Chemistry.Quantities;
using ReactionLab.Chemistry.Stoichiometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Stoichiometry;

public sealed class ReactionOutcomeTests
{
    [Fact]
    public void For_IdentifiesTheReactantThatRunsOutFirst()
    {
        var outcome = ReactionOutcome.For(Moles(4m, 1m), Species.Balanced("H2 O2", "H2O"));

        outcome.LimitingReactantIndex.ShouldBe(1);
        outcome.Extent.Moles.ShouldBe(1m);
    }

    [Fact]
    public void For_ScalesProductsByTheExtentOfReaction() =>
        ReactionOutcome.For(Moles(4m, 1m), Species.Balanced("H2 O2", "H2O"))
            .ProductAmounts.Select(amount => amount.Moles).ShouldBe([2m]);

    [Fact]
    public void For_ReportsWhatIsLeftOfEachReactant() =>
        ReactionOutcome.For(Moles(4m, 1m), Species.Balanced("H2 O2", "H2O"))
            .ExcessReactants.Select(amount => amount.Moles).ShouldBe([2m, 0m]);

    [Fact]
    public void For_LeavesNoExcessOfTheLimitingReactant()
    {
        var outcome = ReactionOutcome.For(Moles(8m, 2m), Species.Balanced("Fe O2", "Fe2O3"));

        outcome.LimitingReactantIndex.ShouldBe(1);
        outcome.ExcessReactants[1].Moles.ShouldBe(0m);
    }

    [Fact]
    public void For_StopsAtZeroExtentWhenAReactantIsAbsent()
    {
        var outcome = ReactionOutcome.For(Moles(4m, 0m), Species.Balanced("H2 O2", "H2O"));

        outcome.Extent.Moles.ShouldBe(0m);
        outcome.ProductAmounts.Select(amount => amount.Moles).ShouldBe([0m]);
    }

    [Fact]
    public void For_RejectsAmountsThatDoNotMatchTheEquation() =>
        Should.Throw<ArgumentException>(() =>
            _ = ReactionOutcome.For(Moles(1m), Species.Balanced("H2 O2", "H2O")));

    [Fact]
    public void For_ReportsNoLimitingReactantWhenTheMixtureIsStoichiometric()
    {
        var outcome = ReactionOutcome.For(Moles(4m, 2m), Species.Balanced("H2 O2", "H2O"));

        outcome.LimitingReactantIndex.ShouldBeNull();
        outcome.Extent.Moles.ShouldBe(2m);
        outcome.ExcessReactants.Select(amount => amount.Moles).ShouldBe([0m, 0m]);
    }

    private static IReadOnlyList<Amount> Moles(params decimal[] values) =>
        [.. values.Select(Amount.FromMoles)];
}
