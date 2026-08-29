using ReactionLab.Chemistry.Quantities;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Quantities;

public sealed class QuantityTests
{
    private const decimal Water = 18.015m;

    [Fact]
    public void Factories_RejectNegativeQuantities()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _ = Mass.FromGrams(-1m));
        Should.Throw<ArgumentOutOfRangeException>(() => _ = Amount.FromMoles(-1m));
        Should.Throw<ArgumentOutOfRangeException>(() => _ = Volume.FromLiters(-1m));
        Should.Throw<ArgumentOutOfRangeException>(() => _ = Molarity.FromMolesPerLiter(-1m));
    }

    [Fact]
    public void ToAmount_DividesMassByMolarMass() =>
        Mass.FromGrams(36.03m).ToAmount(Water).Moles.ShouldBe(2m);

    [Fact]
    public void ToMass_MultipliesAmountByMolarMass() =>
        Amount.FromMoles(2m).ToMass(Water).Grams.ShouldBe(36.03m);

    [Fact]
    public void AsPercentOf_ReportsTheFractionOfTheWhole() =>
        Mass.FromGrams(4.5m).AsPercentageOf(Mass.FromGrams(6m)).ShouldBe(75m);

    [Fact]
    public void FromMilliliters_ConvertsToLiters() =>
        Volume.FromMilliliters(250m).Liters.ShouldBe(0.25m);

    [Fact]
    public void Of_DividesSoluteByTheVolumeOfSolution() =>
        Molarity.Of(Amount.FromMoles(0.5m), Volume.FromLiters(2m)).MolesPerLiter.ShouldBe(0.25m);

    [Fact]
    public void In_ConservesSoluteWhenASolutionIsDiluted()
    {
        var solute = Molarity.FromMolesPerLiter(0.5m).In(Volume.FromMilliliters(100m));

        solute.Moles.ShouldBe(0.05m);
        Molarity.Of(solute, Volume.FromMilliliters(500m)).MolesPerLiter.ShouldBe(0.1m);
    }
}
