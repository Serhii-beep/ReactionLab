using ReactionLab.Catalog.Application.Features.Releases.Reads;
using ReactionLab.Catalog.Application.Features.Releases.Search;
using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Infrastructure.Persistence;
using ReactionLab.Chemistry.Constitution;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Infrastructure.UnitTests.Releases;

public sealed class CatalogKeysTests
{
    [Theory]
    [InlineData("  Sodium   Chloride ", "sodium chloride")]
    [InlineData("H2SO4", "h2so4")]
    [InlineData("ВОДА", "вода")]
    public void Normalize_TrimsCollapsesAndLowers(string text, string expected) =>
        SearchKeys.Normalize(text).ShouldBe(expected);

    [Theory]
    [InlineData("ΟΔΟΣ", "οδοσ")]
    [InlineData("İzmir", "İzmir")]
    public void Normalize_LowersEachCharacterWithoutContext(string text, string expected) =>
        SearchKeys.Normalize(text).ShouldBe(expected);

    [Theory]
    [InlineData("2-methoxyhexanol", "methoxyhexanol")]
    [InlineData("sodium chloride", "chloride")]
    [InlineData("iron ii", "iron")]
    [InlineData("iron lead", "iron")]
    [InlineData("--", null)]
    public void LongestWord_TakesTheFirstOfTheLongest(string key, string? expected) =>
        SearchKeys.LongestWord(key).ShouldBe(expected);

    [Fact]
    public void Normalize_ComposesToNfc() =>
        SearchKeys.Normalize("cafe\u0301").ShouldBe("caf\u00e9");

    [Fact]
    public void Normalize_ComposesAgainAfterLowering()
    {
        var key = SearchKeys.Normalize("J\u030C");

        key.ShouldBe("\u01F0");
        SearchKeys.IsNormalized(key).ShouldBeTrue();
    }

    [Fact]
    public void Buckets_HoldShortPrefixesAndTheWholeWord() =>
        SearchKeys.Buckets("chloride").ShouldBe(["ch", "chl", "chloride"]);

    [Fact]
    public void Words_SplitOnAnythingButLettersAndDigits() =>
        SearchKeys.Words("2-methylpropan-1-ol").ShouldBe(["2", "methylpropan", "1", "ol"]);

    [Fact]
    public void SubstanceKey_PrefersCidThenStructureThenFormulaAndName()
    {
        NaturalKeys.ForSubstance(962, "abc", "H2O", "Water").ShouldBe("cid:962");
        NaturalKeys.ForSubstance(null, "abc", "H2O", "Water").ShouldBe("structure:abc");
        NaturalKeys.ForSubstance(null, null, "H2O", " Water ").ShouldBe("formula:H2O|water");
    }

    [Fact]
    public void WeakerKeys_ListTheTiersBelowTheSubstancesOwnKey()
    {
        NaturalKeys.WeakerKeysForSubstance(962, "abc", "H2O", "Water").ShouldBe(["structure:abc", "formula:H2O|water"]);
        NaturalKeys.WeakerKeysForSubstance(962, null, "H2O", "Water").ShouldBe(["formula:H2O|water"]);
        NaturalKeys.WeakerKeysForSubstance(null, "abc", "H2O", "Water").ShouldBe(["formula:H2O|water"]);
        NaturalKeys.WeakerKeysForSubstance(null, null, "H2O", "Water").ShouldBeEmpty();
    }

    [Fact]
    public void ReactionKey_HashesALongEquationWithoutLosingTheRule()
    {
        var participants = Enumerable.Range(0, 20)
            .Select(_ => new NaturalKeys.ReactionSide(Guid.CreateVersion7(), ParticipantRole.Reactant, 1))
            .ToList();

        var curated = NaturalKeys.ForReaction(participants, "curated");
        var generated = NaturalKeys.ForReaction(participants, "generated");

        curated.Length.ShouldBeLessThanOrEqualTo(NaturalKeys.MaximumLength);
        curated.ShouldStartWith("sha256:");
        generated.ShouldNotBe(curated);
    }

    [Fact]
    public void ReactionKey_IgnoresTheOrderOfParticipants()
    {
        var hydrogen = Guid.CreateVersion7();
        var oxygen = Guid.CreateVersion7();
        var water = Guid.CreateVersion7();

        var written = NaturalKeys.ForReaction(
            [new(hydrogen, ParticipantRole.Reactant, 2), new(oxygen, ParticipantRole.Reactant, 1), new(water, ParticipantRole.Product, 2)],
            "curated");
        var shuffled = NaturalKeys.ForReaction(
            [new(water, ParticipantRole.Product, 2), new(oxygen, ParticipantRole.Reactant, 1), new(hydrogen, ParticipantRole.Reactant, 2)],
            "curated");

        shuffled.ShouldBe(written);
    }

    [Fact]
    public void ConstitutionKey_IgnoresAtomOrder()
    {
        var ethanol = ConstitutionKey.Of(["C", "C", "O"], [new AtomBond(0, 1, 1), new AtomBond(1, 2, 1)]);
        var reordered = ConstitutionKey.Of(["O", "C", "C"], [new AtomBond(2, 1, 2), new AtomBond(1, 0, 1)]);

        reordered.ShouldBe(ethanol);
        ethanol.Length.ShouldBe(ConstitutionKey.Length);
    }

    [Fact]
    public void ConstitutionKey_SeparatesIsomers()
    {
        var ethanol = ConstitutionKey.Of(["C", "C", "O"], [new AtomBond(0, 1, 1), new AtomBond(1, 2, 1)]);
        var dimethylEther = ConstitutionKey.Of(["C", "O", "C"], [new AtomBond(0, 1, 1), new AtomBond(1, 2, 1)]);

        dimethylEther.ShouldNotBe(ethanol);
    }

    [Fact]
    public void ConstitutionKey_SeparatesWhereTheHydrogensSit()
    {
        var acetaldehyde = ConstitutionKey.Of(
            ["C", "C", "O", "H", "H", "H", "H"],
            [new AtomBond(0, 1, 1), new AtomBond(1, 2, 2), new AtomBond(0, 3, 1), new AtomBond(0, 4, 1), new AtomBond(0, 5, 1), new AtomBond(1, 6, 1)]);
        var vinylAlcohol = ConstitutionKey.Of(
            ["C", "C", "O", "H", "H", "H", "H"],
            [new AtomBond(0, 1, 2), new AtomBond(1, 2, 1), new AtomBond(0, 3, 1), new AtomBond(0, 4, 1), new AtomBond(1, 5, 1), new AtomBond(2, 6, 1)]);

        vinylAlcohol.ShouldNotBe(acetaldehyde);
    }

    [Theory]
    [InlineData(new[] { "01a057af-906b-7060-9a72-376b661305dc", "01a057af-906b-72c1-b50e-74fdea74117c" }, true)]
    [InlineData(new[] { "01a057af-906b-72c1-b50e-74fdea74117c", "01a057af-906b-7060-9a72-376b661305dc" }, false)]
    [InlineData(new[] { "01a057af-906b-7060-9a72-376b661305dc", "01a057af-906b-7060-9a72-376b661305dc" }, false)]
    public void Bench_IsCanonicalOnlyWhenSortedAndDistinct(string[] ids, bool canonical) =>
        MatchReleaseReactionsHandler.IsCanonical([.. ids.Select(Guid.Parse)]).ShouldBe(canonical);
}
