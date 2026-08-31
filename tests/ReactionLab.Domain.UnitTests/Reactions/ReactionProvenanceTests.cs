using ReactionLab.Domain.Reactions;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class ReactionProvenanceTests
{
    [Fact]
    public void Create_KeepsTheRuleCodeAndConfidence()
    {
        var provenance = ReactionProvenance.Create("precipitation.halides", 0.85m).Value;

        provenance.Rule.ShouldBe("precipitation.halides");
        provenance.Confidence.ShouldBe(0.85m);
        provenance.IsCurated.ShouldBeFalse();
    }

    [Fact]
    public void Curated_IsFullyConfidentAndSaysSo()
    {
        ReactionProvenance.Curated.Rule.ShouldBe("curated");
        ReactionProvenance.Curated.Confidence.ShouldBe(1m);
        ReactionProvenance.Curated.IsCurated.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RefusesAReactionThatDoesNotSayWhereItCameFrom(string? rule) =>
        ReactionProvenance.Create(rule, 1m).Error.ShouldBe(ReactionProvenance.RuleRequired);

    [Fact]
    public void Create_RefusesARuleCodeLongerThanTheColumn() =>
        ReactionProvenance.Create(new string('x', ReactionProvenance.MaximumRuleLength + 1), 1m)
            .Error.ShouldBe(ReactionProvenance.RuleTooLong);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_RefusesAConfidenceOutsideZeroToOne(decimal confidence) =>
        ReactionProvenance.Create("combustion.complete", confidence)
            .Error.ShouldBe(ReactionProvenance.ConfidenceOutOfRange);

    [Fact]
    public void Create_TrimsTheRuleCode() =>
        ReactionProvenance.Create("  synthesis.metalAndNonMetal  ", 0.9m).Value.Rule
            .ShouldBe("synthesis.metalAndNonMetal");
}
