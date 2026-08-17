using ReactionLab.Domain.Reactions;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class ReactionContentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name) =>
        ReactionContent.Create(name).Error.ShouldBe(ReactionContent.NameRequired);

    [Fact]
    public void Create_RejectsTooLongName() =>
        ReactionContent.Create(new string('x', ReactionContent.MaximumNameLength + 1))
            .Error.ShouldBe(ReactionContent.NameTooLong);

    [Fact]
    public void Create_TrimsAndDiscardsBlankExamples()
    {
        var content = ReactionContent.Create("Test", realWorldExamples: ["   Test   ", "", "   "]).Value;

        content.RealWorldExamples.ShouldHaveSingleItem().ShouldBe("Test");
    }

    [Fact]
    public void WithFallback_MergesValueByValue()
    {
        var original = ReactionContent.Create("Test_Original", "Test_Original", "Test_Original", "Test_Original").Value;
        var translated = ReactionContent.Create("Test_Translated", mechanism: "Test_Translated").Value;

        var merged = translated.WithFallback(original);

        merged.Name.ShouldBe("Test_Translated");
        merged.Mechanism.ShouldBe("Test_Translated");
        merged.Description.ShouldBe("Test_Original");
        merged.SafetyWarnings.ShouldBe("Test_Original");
    }
}
