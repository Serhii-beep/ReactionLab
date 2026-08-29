using ReactionLab.Chemistry.Thermochemistry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.ThermoChemistry;

public sealed class ReactionEnergeticsTests
{
    private static readonly Dictionary<string, StandardState> Table = new(StringComparer.Ordinal)
    {
        ["H2(g)"] = new(0m, 130.7m),
        ["O2(g)"] = new(0m, 205.2m),
        ["N2(g)"] = new(0m, 191.6m),
        ["Cl2(g)"] = new(0m, 223.1m),
        ["Mg(s)"] = new(0m, 32.7m),
        ["H2O(l)"] = new(-285.8m, 70.0m),
        ["CO2(g)"] = new(-393.5m, 213.8m),
        ["CH4(g)"] = new(-74.6m, 186.3m),
        ["NH3(g)"] = new(-45.9m, 192.8m),
        ["HCl(g)"] = new(-92.3m, 186.9m),
        ["C3H8(g)"] = new(-103.8m, 270.3m),
        ["CaCO3(s)"] = new(-1207.6m, 91.7m),
        ["CaO(s)"] = new(-634.9m, 38.1m),
        ["MgO(s)"] = new(-601.6m, 26.9m),
        ["NaOH(aq)"] = new(-470.1m, null),
        ["HCl(aq)"] = new(-167.2m, null),
        ["NaCl(aq)"] = new(-407.3m, null)
    };

    [Theory]
    [InlineData("H2(g) O2(g)", "H2O(l)", -571.6, -474.2)]
    [InlineData("N2(g) H2(g)", "NH3(g)", -91.8, -32.8)]
    [InlineData("CH4(g) O2(g)", "CO2(g) H2O(l)", -890.5, -818.0)]
    [InlineData("CaCO3(s)", "CaO(s) CO2(g)", 179.2, 131.1)]
    [InlineData("H2(g) Cl2(g)", "HCl(g)", -184.6, -190.5)]
    [InlineData("Mg(s) O2(g)", "MgO(s)", -1203.2, -1138.9)]
    [InlineData("C3H8(g) O2(g)", "CO2(g) H2O(l)", -2219.9, -2108.5)]
    public void ReactionEnergetics_ReproduceLiteratureValues(
        string reactants, string products, decimal enthalpy, decimal gibbs)
    {
        var equation = Species.Balanced(Formulas(reactants), Formulas(products));
        var computed = ReactionEnergetics.EnthalpyOfReaction(equation, States(reactants), States(products));

        computed.ShouldBe(enthalpy, 0.5m);

        ReactionEnergetics.TryEntropyOfReaction(
            equation, States(reactants), States(products), out var entropy).ShouldBeTrue();

        ReactionEnergetics.GibbsFreeEnergy(computed, entropy, 298.15m).ShouldBe(gibbs, 0.5m);
    }

    [Fact]
    public void EnthalpyOfReaction_WOrksWhereNoEntropyIsRecorded() =>
        ReactionEnergetics.EnthalpyOfReaction(
            Species.Balanced("NaOH HCl", "NaCl H2O"),
            States("NaOH(aq) HCl(aq)"),
            States("NaCl(aq) H2O(l)")).ShouldBe(-55.8m, 0.1m);

    [Fact]
    public void TryEntropyOfReaction_RefusesWhenAnySpeciesHasNoEntropy()
    {
        ReactionEnergetics.TryEntropyOfReaction(
            Species.Balanced("NaOH HCl", "NaCl H2O"),
            States("NaOH(aq) HCl(aq)"),
            States("NaCl(aq) H2O(l)"),
            out var entropy).ShouldBeFalse();

        entropy.ShouldBe(0m);
    }

    [Fact]
    public void GibbsFreeEnergy_RejectsANonPositiveTemperature() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = ReactionEnergetics.GibbsFreeEnergy(-571.6m, -326.6m, 0m));

    [Fact]
    public void EnthalpyOfReaction_RejectsStatesThatDoNotMatchTheEquation() =>
        Should.Throw<ArgumentException>(() =>
            _ = ReactionEnergetics.EnthalpyOfReaction(
                Species.Balanced("H2 O2", "H2O"), States("H2(g)"), States("H2O(l)")));

    [Theory]
    [InlineData(-100, 80, 0.25, 55)]
    [InlineData(-600, 80, 0.25, 0)]
    [InlineData(100, 100, 0.5, 150)]
    [InlineData(200, 50, 0.5, 200)]
    public void EstimateActivationEnergy_NeverFallsBelowTheProducts(
        decimal enthalpy, decimal barrier, decimal transferCoefficient, decimal expected) =>
        ReactionEnergetics.EstimateActivationEnergy(enthalpy, barrier, transferCoefficient)
            .ShouldBe(expected);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void EstimateActivationEnergy_RejectsATransferCoefficientOutsideZeroToOne(
        decimal transferCoefficient) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = ReactionEnergetics.EstimateActivationEnergy(-100m, 80m, transferCoefficient));

    private static string Formulas(string species) =>
        string.Join(' ', Parts(species).Select(part => part.Formula));

    private static IReadOnlyList<StandardState> States(string species) =>
        [.. Parts(species).Select(part => Table[part.Whole])];

    private static IEnumerable<(string Whole, string Formula)> Parts(string species) =>
        species.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => (part, part[..part.IndexOf('(', StringComparison.Ordinal)]));
}
