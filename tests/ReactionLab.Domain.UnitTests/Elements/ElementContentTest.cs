using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class ElementContentTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name) =>
        ElementContent.Create(name).Error.ShouldBe(ElementContent.NameRequired);

    [Fact]
    public void Create_RejectsTooLongName() =>
        ElementContent.Create(new string('x', ElementContent.MaximumNameLength + 1))
            .Error.ShouldBe(ElementContent.NameTooLong);

    [Fact]
    public void Create_TrimsName() =>
        ElementContent.Create("   Test   ").Value.Name.ShouldBe("Test");

    [Fact]
    public void Create_TrimsAndSicardsBlankFacts()
    {
        var content = ElementContent.Create(
            "Test",
            "   Test   ",
            ["   Test   ", "", "   "]).Value;

        content.DiscoveryInfo.ShouldBe("Test");
        content.InterestingFacts.ShouldHaveSingleItem().ShouldBe("Test");
    }

    [Fact]
    public void WithFallback_KeepsTranslatedValuesAndFillsGaps()
    {
        var original = ElementContent.Create("Test_Original", "Test_Original", ["Test_Original"]).Value;
        var translated = ElementContent.Create("Test_Translated").Value;

        var merged = translated.WithFallback(original);

        merged.Name.ShouldBe("Test_Translated");
        merged.DiscoveryInfo.ShouldBe("Test_Original");
        merged.InterestingFacts.ShouldBe(["Test_Original"]);
    }
}
