using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class ElementSymbolTests
{
    [Theory]
    [InlineData("H")]
    [InlineData("He")]
    [InlineData("Uup")]
    [InlineData("   Fe   ")]
    public void Create_AcceptsValidSymbols(string value) =>
        ElementSymbol.Create(value).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("h")]
    [InlineData("HE")]
    [InlineData("Heli")]
    [InlineData("H2")]
    [InlineData("H-")]
    public void Create_RejectsInvalidSymbols(string? value)
    {
        var result = ElementSymbol.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ElementSymbol.Invalid);
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace() =>
        ElementSymbol.Create("   Na   ").Value.Value.ShouldBe("Na");
}
