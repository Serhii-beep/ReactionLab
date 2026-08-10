using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class AtomicMassTests
{
    [Fact]
    public void Create_AcceptsPositiveMass() =>
        AtomicMass.Create(1.008m).Value.Daltons.ShouldBe(1.008m);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveMass(decimal daltons)
    {
        var result = AtomicMass.Create(daltons);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AtomicMass.NotPositive);
    }

    [Fact]
    public void Create_RejectsImplausiblyLargeMass()
    {
        var result = AtomicMass.Create(1000m);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AtomicMass.Implausible);
    }
}
