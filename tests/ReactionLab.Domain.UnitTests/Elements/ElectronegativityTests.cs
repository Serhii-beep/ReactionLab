using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class ElectronegativityTests
{
    [Theory]
    [InlineData(0.79)]
    [InlineData(2.20)]
    [InlineData(3.98)]
    public void Create_AcceptsValuesOnThePaulingScale(decimal pauling) =>
        Electronegativity.Create(pauling).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(4.5)]
    [InlineData(-1)]
    public void Create_RejectsValuesOffTheScale(decimal pauling)
    {
        var result = Electronegativity.Create(pauling);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Electronegativity.OutOfRange);
    }
}
