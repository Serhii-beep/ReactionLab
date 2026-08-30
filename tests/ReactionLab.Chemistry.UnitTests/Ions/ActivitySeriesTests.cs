using ReactionLab.Chemistry.Ions;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Ions;

public sealed class ActivitySeriesTests
{
    [Theory]
    [InlineData("Zn", "Cu", true)]
    [InlineData("Cu", "Zn", false)]
    [InlineData("Zn", "H", true)]
    [InlineData("Cu", "H", false)]
    [InlineData("K", "Na", true)]
    [InlineData("Al", "Fe", true)]
    [InlineData("Fe", "Al", false)]
    [InlineData("Cu", "Cu", false)]
    public void Displaces_FollowsTheOrderOfTheSeries(string first, string second, bool displaces) =>
        TestIons.Series.Displaces(first, second).ShouldBe(displaces);

    [Theory]
    [InlineData("Xx", "Cu")]
    [InlineData("Zn", "Xx")]
    public void Displaces_RefusesAMetalTheSeriesDoesNotHold(string first, string second) =>
        TestIons.Series.Displaces(first, second).ShouldBeFalse();

    [Theory]
    [InlineData("Na", 1, WaterReactivity.Cold)]
    [InlineData("Fe", 2, WaterReactivity.Steam)]
    [InlineData("Al", 3, WaterReactivity.Steam)]
    [InlineData("Cu", 2, WaterReactivity.None)]
    public void TryFind_CarriesTheIonTheMetalFormsAndWhatItDoesWithWater(
        string symbol, int charge, WaterReactivity water)
    {
        TestIons.Series.TryFind(symbol, out var metal).ShouldBeTrue(symbol);

        metal.ShouldBe(new ActiveMetal(symbol, charge, water));
    }
}
