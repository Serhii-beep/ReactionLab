using ReactionLab.Chemistry.Quantities;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Quantities;

public sealed class IdealGasTests
{
    [Theory]
    [InlineData(273.15, 101.325, 22.414)]
    [InlineData(273.15, 100, 22.711)]
    [InlineData(298.15, 100, 24.79)]
    public void VolumeOf_ReproducesTheStandardMolarVolumes(
        decimal kelvin, decimal kilopascals, decimal liters) =>
        Math.Round(IdealGas.VolumeOf(Amount.FromMoles(1m), kelvin, kilopascals).Liters, 3)
            .ShouldBe(liters);

    [Fact]
    public void AmountIn_InvertsVolumeOf()
    {
        var volume = IdealGas.VolumeOf(Amount.FromMoles(2.5m), 298.15m, 100m);

        Math.Round(IdealGas.AmountIn(volume, 298.15m, 100m).Moles, 6).ShouldBe(2.5m);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(298.15, 0)]
    public void VolumeOf_RejectsImpossibleConditions(decimal kelvin, decimal kilopascals) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = IdealGas.VolumeOf(Amount.FromMoles(1m), kelvin, kilopascals));
}
