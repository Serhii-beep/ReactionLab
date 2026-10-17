using System.Globalization;
using ReactionLab.Chemistry.Matching;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Matching;

public sealed class ReactantMatchingTests
{
    private static readonly ReactantRequirement<string>[] WaterSynthesis = [new("H2", 2), new("O2", 1)];

    [Theory]
    [InlineData("H2:4 O2:2", 2)]
    [InlineData("H2:4 O2:1", 1)]
    [InlineData("H2:2 O2:9", 1)]
    [InlineData("H2:1 O2:1", 0)]
    [InlineData("H2:4", 0)]
    [InlineData("", 0)]
    public void Match_CountsHowManyTimesTheReactionCanRun(string available, int runs) =>
        Match(available).Runs.ShouldBe(runs);

    [Theory]
    [InlineData("H2:4 O2:1", 1)]
    [InlineData("H2:2", 0.667)]
    [InlineData("O2:1", 0.333)]
    [InlineData("H2:1", 0.333)]
    [InlineData("", 0)]
    public void Match_ScoresCompletenessByUnits(string available, decimal score) =>
        Math.Round(Match(available).Completeness, 3).ShouldBe(score);

    [Fact]
    public void Match_ListsEachShortfallWithTheAmountToAdd() =>
        Match("H2:1").Shortfall.ShouldBe([
            new ReactantShortfall<string>("H2", 1),
            new ReactantShortfall<string>("O2", 1)]);

    [Fact]
    public void Match_ReportsNoShortfallWhenTheReactionCanRun() =>
        Match("H2:2 O2:1").Shortfall.ShouldBeEmpty();

    [Fact]
    public void Match_IgnoresSubstancesTheReactionDoesNotUse()
    {
        var match = Match("H2:2 O2:1 NaCl:50");

        match.Runs.ShouldBe(1);
        match.Completeness.ShouldBe(1m);
    }

    [Fact]
    public void Match_TreatsANegativeCountAsNone() =>
        Match("H2:-5 O2:1").Shortfall.ShouldBe([new ReactantShortfall<string>("H2", 2)]);

    [Fact]
    public void Match_RejectsAReactionWithNoReactants() =>
        Should.Throw<ArgumentException>(() =>
            _ = ReactantMatching.Match([], Bag("H2:2")));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Match_RejectsANonPositiveCoefficient(int coefficient) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = ReactantMatching.Match([new("H2", coefficient)], Bag("H2:2")));

    private static ReactionMatch<string> Match(string available) =>
        ReactantMatching.Match(WaterSynthesis, Bag(available));

    private static Dictionary<string, int> Bag(string available) =>
        available.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Split(':'))
            .ToDictionary(
                parts => parts[0],
                parts => int.Parse(parts[1], CultureInfo.InvariantCulture),
                StringComparer.Ordinal);
}
