using ReactionLab.Domain.Substances;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Substances;

public sealed class SubstanceContentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name) =>
        SubstanceContent.Create(name).Error.ShouldBe(SubstanceContent.NameRequired);

    [Fact]
    public void Create_RejectsTooLongName() =>
        SubstanceContent.Create(new string('x', SubstanceContent.MaximumNameLength + 1))
            .Error.ShouldBe(SubstanceContent.NameTooLong);

    [Fact]
    public void Create_TrimsAndDiscardsBlankCommonNames()
    {
        var content = SubstanceContent.Create(
            "Test",
            iupacName: "Test",
            description: "Test",
            commonNames: ["   Test1   ", "", "Test2"]).Value;

        content.IupacName.ShouldBe("Test");
        content.CommonNames.ShouldBe(["Test1", "Test2"]);
        content.Description.ShouldBe("Test");
    }

    [Fact]
    public void Create_TruncatesATooLongIupacName() =>
        SubstanceContent.Create("Test", new string('x', SubstanceContent.MaximumIupacNameLength + 1))
            .Value.IupacName!.Length.ShouldBe(SubstanceContent.MaximumIupacNameLength);

    [Fact]
    public void WithFallback_MergesValueByValue()
    {
        var original = SubstanceContent.Create(
            "Test_Original", "Test_Original", "Test_Original", "Test_Original", ["Test_Original"]).Value;
        var translated = SubstanceContent.Create("Test_Translated", description: "Test_Translated").Value;

        var merged = translated.WithFallback(original);

        merged.Name.ShouldBe("Test_Translated");
        merged.Description.ShouldBe("Test_Translated");
        merged.IupacName.ShouldBe("Test_Original");
        merged.SafetyInformation.ShouldBe("Test_Original");
        merged.CommonNames.ShouldBe(["Test_Original"]);
    }
}
