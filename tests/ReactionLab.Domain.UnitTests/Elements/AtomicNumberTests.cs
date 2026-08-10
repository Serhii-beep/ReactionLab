using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class AtomicNumberTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(26)]
    [InlineData(118)]
    public void Create_AcceptsValuesInRange(int value) =>
        AtomicNumber.Create(value).Value.Value.ShouldBe(value);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(119)]
    public void Create_RejectsValuesOutOfRange(int value)
    {
        var result = AtomicNumber.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AtomicNumber.OutOfRange);
    }
}
