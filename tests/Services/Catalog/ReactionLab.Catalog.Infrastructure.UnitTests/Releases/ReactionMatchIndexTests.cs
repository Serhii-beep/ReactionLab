using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Infrastructure.Releases.Matching;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Infrastructure.UnitTests.Releases;

public sealed class ReactionMatchIndexTests
{
    [Fact]
    public void Match_EqualsAFullScan_OnRandomCatalogs()
    {
        var random = new Random(7);
        var substances = Enumerable.Range(0, 60).Select(_ => Guid.CreateVersion7()).ToArray();
        var reactions = Enumerable.Range(0, 2000)
            .Select(_ => (Id: Guid.CreateVersion7(), Reactants: Pick(random, substances, 1 + random.Next(3))))
            .ToList();
        var index = ReactionMatchIndex.Build(reactions);

        for (var trial = 0; trial < 200; trial++)
        {
            var bench = Pick(random, substances, 1 + random.Next(8));

            foreach (var match in new[] { ReactantMatch.Partial, ReactantMatch.Complete })
            {
                index.Match(bench, match, 30).ShouldBe(Scan(reactions, bench, match, 30));
            }
        }
    }

    [Fact]
    public void Match_RanksByMissingReactantsThenByRank()
    {
        var water = Guid.CreateVersion7();
        var oxygen = Guid.CreateVersion7();
        var carbon = Guid.CreateVersion7();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var third = Guid.CreateVersion7();

        var index = ReactionMatchIndex.Build([(first, [water, carbon]), (second, [oxygen]), (third, [water, oxygen])]);

        index.Match([water, oxygen], ReactantMatch.Partial, 10).ShouldBe(
        [
            new MatchedReaction(second, 0),
            new MatchedReaction(third, 0),
            new MatchedReaction(first, 1)
        ]);
    }

    [Fact]
    public void Match_IgnoresSubstancesNoReactionUses()
    {
        var oxygen = Guid.CreateVersion7();
        var burn = Guid.CreateVersion7();
        var index = ReactionMatchIndex.Build([(burn, [oxygen])]);

        index.Match([Guid.CreateVersion7()], ReactantMatch.Partial, 10).ShouldBeEmpty();
        index.Match([oxygen, Guid.CreateVersion7()], ReactantMatch.Complete, 10).ShouldBe([new MatchedReaction(burn, 0)]);
    }

    [Fact]
    public void Match_StaysCorrectAcrossRepeatedCalls()
    {
        var oxygen = Guid.CreateVersion7();
        var hydrogen = Guid.CreateVersion7();
        var water = Guid.CreateVersion7();
        var index = ReactionMatchIndex.Build([(water, [hydrogen, oxygen])]);

        index.Match([hydrogen], ReactantMatch.Partial, 10).ShouldBe([new MatchedReaction(water, 1)]);
        index.Match([hydrogen, oxygen], ReactantMatch.Partial, 10).ShouldBe([new MatchedReaction(water, 0)]);
        index.Match([oxygen], ReactantMatch.Complete, 10).ShouldBeEmpty();
    }

    private static Guid[] Pick(Random random, Guid[] from, int count) =>
        [.. from.OrderBy(_ => random.Next()).Take(count)];

    private static List<MatchedReaction> Scan(
        List<(Guid Id, Guid[] Reactants)> reactions,
        IReadOnlyCollection<Guid> bench,
        ReactantMatch match,
        int limit)
    {
        var available = bench.ToHashSet();

        return [.. reactions
            .Select((reaction, rank) => (reaction.Id, Rank: rank, Present: reaction.Reactants.Count(available.Contains), reaction.Reactants.Length))
            .Where(entry => entry.Present > 0 && (match == ReactantMatch.Partial || entry.Present == entry.Length))
            .OrderBy(entry => entry.Length - entry.Present)
            .ThenBy(entry => entry.Rank)
            .Take(limit)
            .Select(entry => new MatchedReaction(entry.Id, entry.Length - entry.Present))];
    }
}
