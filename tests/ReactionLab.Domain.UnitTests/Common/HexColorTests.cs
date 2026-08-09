using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class HexColorTests
{
    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#4fc3f7")]
    [InlineData("   #D9FFFF   ")]
    public void Create_AcceptsValidSixDigitHex(string value)
    {
        HexColor.Create(value).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FFFFFF")]
    [InlineData("#FFF")]
    [InlineData("#FFFFFFF")]
    [InlineData("#GGGGGG")]
    public void Create_RejectsMalformedValues(string? value)
    {
        var result = HexColor.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(HexColor.Invalid);
    }

    [Fact]
    public void Create_NormalizesToUppercase()
    {
        HexColor.Create("#4fc3f7").Value.Value.ShouldBe("#4FC3F7");
    }

    [Fact]
    public void ColorsDifferingOnlyInCase_AreEqual()
    {
        HexColor.Create("#4fc3f7").Value.ShouldBe(HexColor.Create("#4FC3F7").Value);
    }
}
