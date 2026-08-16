using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.SharedKernel;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class ThermodynamicsTests
{
    [Fact]
    public void Unknown_HasNeitherEnthalpyNorBarrier()
    {
        Thermodynamics.Unknown.EnthalpyChange.ShouldBeNull();
        Thermodynamics.Unknown.IsExothermic.ShouldBeNull();
        Thermodynamics.Unknown.ReverseActivationEnergyKilojoulesPerMole.ShouldBeNull();
    }

    [Fact]
    public void ExothermicReaction_DerivesItsSignFromEnthalpy()
    {
        var energetics = Thermodynamics.Create(CreateEnthalpy(-890), 150).Value;

        energetics.IsExothermic.ShouldBe(true);
        energetics.IsEndothermic.ShouldBe(false);
    }

    [Fact]
    public void ReverseBarrier_IsForwardBarrierMinusEnthalpy()
    {
        var energetics = Thermodynamics.Create(CreateEnthalpy(-572), 75).Value;

        energetics.ReverseActivationEnergyKilojoulesPerMole.ShouldBe(647);
    }

    [Fact]
    public void EndothermicReaction_MayHaveABarrierAboveItsEnthalpy()
    {
        Thermodynamics.Create(CreateEnthalpy(572), 647).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void EndothermicReaction_CannotHaveABarrierBelowItsEnthalpy()
    {
        var result = Thermodynamics.Create(CreateEnthalpy(572), 285);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Thermodynamics.ActivationEnergyBelowEnthalpy);
    }

    [Fact]
    public void ExothermicReaction_MayHaveASmallBarrier()
    {
        Thermodynamics.Create(CreateEnthalpy(-572), 1).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void NegativeActivationEnergy_IsRejected()
    {
        Thermodynamics.Create(CreateEnthalpy(-100), -1).Error.ShouldBe(Thermodynamics.NegativeActivationEnergy);
    }

    [Fact]
    public void ImplausibleActivationEnergy_IsRejected()
    {
        Thermodynamics.Create(null, 200000).Error.ShouldBe(Thermodynamics.ImplausibleActivationEnergy);
    }

    private static Enthalpy CreateEnthalpy(decimal kilojoulesPerMole) =>
        Enthalpy.FromKilojoulesPerMole(kilojoulesPerMole).Value;
}
