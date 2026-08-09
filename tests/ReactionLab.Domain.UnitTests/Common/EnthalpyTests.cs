using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class EnthalpyTests
{
    [Fact]
    public void NegativeEnthalpy_IsExothermic()
    {
        var enthalpy = Enthalpy.FromKilojoulesPerMole(-890).Value;

        enthalpy.IsExothermic.ShouldBeTrue();
        enthalpy.IsEndothermic.ShouldBeFalse();
    }

    [Fact]
    public void PositiveEnthalpy_IsEndothermic()
    {
        var enthalpy = Enthalpy.FromKilojoulesPerMole(178).Value;

        enthalpy.IsEndothermic.ShouldBeTrue();
        enthalpy.IsExothermic.ShouldBeFalse();
    }

    [Fact]
    public void ZeroEnthalpy_IsNeitherExoNorEndothermic()
    {
        var enthalpy = Enthalpy.FromKilojoulesPerMole(0).Value;

        enthalpy.IsThermoneutral.ShouldBeTrue();
        enthalpy.IsExothermic.ShouldBeFalse();
        enthalpy.IsEndothermic.ShouldBeFalse();
    }

    [Theory]
    [InlineData(-572000)]
    [InlineData(150000)]
    public void ImplausibleMagnitude_IsRejected(decimal value)
    {
        var result = Enthalpy.FromKilojoulesPerMole(value);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Enthalpy.Implausible);
    }
}
