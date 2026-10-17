using ReactionLab.Chemistry.Formulas;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Formulas;

public sealed class MolarMassTests
{
    private static readonly Dictionary<string, decimal> Masses = new(StringComparer.Ordinal)
    {
        ["H"] = 1.008m,
        ["C"] = 12.011m,
        ["N"] = 14.007m,
        ["O"] = 15.999m,
        ["Na"] = 22.990m,
        ["S"] = 32.06m,
        ["Cl"] = 35.45m,
        ["Ca"] = 40.078m,
        ["Fe"] = 55.845m
    };

    [Theory]
    [InlineData("Fe", 55.845)]
    [InlineData("H2O", 18.015)]
    [InlineData("CO2", 44.009)]
    [InlineData("NaCl", 58.440)]
    [InlineData("H2SO4", 98.072)]
    [InlineData("Ca(OH)2", 74.092)]
    [InlineData("C6H12O6", 180.156)]
    public void TryCompute_SumsAtomicMassTimesCount(string formula, decimal expected)
    {
        MolarMass.TryCompute(Parse(formula), Masses, out var grams, out _).ShouldBeTrue();

        grams.ShouldBe(expected);
    }

    [Fact]
    public void TryCompute_NamesTHeElementItHasNoMassFor()
    {
        MolarMass.TryCompute(Parse("NaBr"), Masses, out var grams, out var unknown).ShouldBeFalse();

        unknown.ShouldBe("Br");
        grams.ShouldBe(0m);
    }

    [Fact]
    public void PercentComposition_MatchesTheKnownMassFractions()
    {
        var water = MolarMass.PercentComposition(Parse("H2O"), Masses);

        water.Select(element => (element.Symbol, Math.Round(element.Percent, 2)))
            .ShouldBe([("H", 11.19m), ("O", 88.81m)]);
    }

    [Fact]
    public void PercentComposition_IsEmptyWhenAnElementIsUnknown() =>
        MolarMass.PercentComposition(Parse("NaBr"), Masses).ShouldBeEmpty();

    private static ChemicalComposition Parse(string formula)
    {
        FormulaParser.TryParse(formula, out var composition, out var error)
            .ShouldBeTrue($"'{formula}' was refused as {error}");

        return composition;
    }
}
