using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class PressureTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FromKilopascals_RejectsNonPositiveValues(decimal kilopascals)
    {
        var result = Pressure.FromKilopascals(kilopascals);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Pressure.NotPositive);
    }

    [Fact]
    public void FromKilopascals_AcceptsPositiveValues()
    {
        Pressure.FromKilopascals(101.325m).Value.Kilopascals.ShouldBe(101.325m);
    }

    [Fact]
    public void OneAtmosphere_IsStandardPressure()
    {
        Pressure.FromAtmospheres(1).Value.Kilopascals.ShouldBe(101.325m);
    }

    [Fact]
    public void Atmospheres_RoundTripsWithKilopascals()
    {
        Pressure.StandardAtmosphere.Atmospheres.ShouldBe(1m);
    }
}
