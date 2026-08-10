using ReactionLab.Domain.Elements;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Elements;

public sealed class AtomicRadiiTests
{
    [Fact]
    public void Create_AcceptsCovalentRadiusAlone()
    {
        var radii = AtomicRadii.Create(31m).Value;

        radii.CovalentPicometers.ShouldBe(31m);
        radii.VanDerWaalsPicometers.ShouldBeNull();
    }

    [Fact]
    public void Create_AcceptsVanDerWaalsLargerThanCovalent()
    {
        var radii = AtomicRadii.Create(31m, 120m).Value;

        radii.VanDerWaalsPicometers.ShouldBe(120m);
    }

    [Fact]
    public void Create_RejectsVanDerWaalsSmallerThanCovalent()
    {
        var result = AtomicRadii.Create(120m, 31m);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AtomicRadii.VanDerWaalsSmallerThanCovalent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_RejectsNonPositiveRadius(decimal covalent)
    {
        AtomicRadii.Create(covalent).Error.ShouldBe(AtomicRadii.NotPositive);
    }

    [Fact]
    public void Create_RejectsImplausibilyLargeRadius()
    {
        AtomicRadii.Create(500m).Error.ShouldBe(AtomicRadii.Implausible);
    }
}
