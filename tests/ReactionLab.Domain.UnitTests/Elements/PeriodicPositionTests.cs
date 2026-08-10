using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class PeriodicPositionTests
{
    [Fact]
    public void Create_AcceptsPeriodAndGroup()
    {
        var position = PeriodicPosition.Create(3, 17).Value;

        position.Period.ShouldBe(3);
        position.Group.ShouldBe(17);
        position.IsFBlock.ShouldBeFalse();
    }

    [Fact]
    public void Create_AcceptsMissingGroupForFBlock()
    {
        var position = PeriodicPosition.Create(6, null).Value;

        position.Group.ShouldBeNull();
        position.IsFBlock.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Create_RejectsPeriodOutOfRange(int period)
    {
        var result = PeriodicPosition.Create(period, 1);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PeriodicPosition.PeriodOutOfRange);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)]
    public void Create_RejectsGroupOutOfRange(int group)
    {
        var result = PeriodicPosition.Create(1, group);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(PeriodicPosition.GroupOutOfRange);
    }
}
