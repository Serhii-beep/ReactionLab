using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class TemperatureTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(273.15)]
    [InlineData(5800)]
    public void FromKelvin_AcceptsNonNegativeValues(decimal kelvin)
    {
        var result = Temperature.FromKelvin(kelvin);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Kelvin.ShouldBe(kelvin);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-273.15)]
    public void FromKelvin_RejectsValuesBelowAbsoluteZero(decimal kelvin)
    {
        var result = Temperature.FromKelvin(kelvin);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Temperature.BelowAbsoluteZero);
    }

    [Fact]
    public void FromCelsius_ConvertsToKelvin()
    {
        Temperature.FromCelsius(0).Value.Kelvin.ShouldBe(273.15m);
        Temperature.FromCelsius(100).Value.Kelvin.ShouldBe(373.15m);
    }

    [Fact]
    public void FromCelsius_RejectsBelowAbsoluteZero()
    {
        Temperature.FromCelsius(-273.16m).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void FromCelsius_RoundTripsWIthKelvin()
    {
        Temperature.FromKelvin(298.15m).Value.Celsius.ShouldBe(25m);
    }

    [Fact]
    public void SameTemperature_IsEqual()
    {
        Temperature.FromKelvin(300).Value.ShouldBe(Temperature.FromKelvin(300).Value);
    }
}
