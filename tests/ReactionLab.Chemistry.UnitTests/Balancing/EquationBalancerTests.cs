using System.Globalization;
using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Formulas;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Balancing;

public sealed class EquationBalancerTests
{
    [Theory]
    [InlineData("H2 O2", "H2O", "2 1", "2")]
    [InlineData("Fe O2", "Fe2O3", "4 3", "2")]
    [InlineData("H2O", "H2 O2", "2", "2 1")]
    [InlineData("C3H8 O2", "CO2 H2O", "1 5", "3 4")]
    [InlineData("Ca(OH)2 H3PO4", "Ca3(PO4)2 H2O", "3 2", "1 6")]
    [InlineData("KMnO4 HCl", "KCl MnCl2 H2O Cl2", "2 16", "2 2 8 5")]
    public void TryBalance_BalancesMolecularEquations(
        string reactants, string products, string reactantCoefficients, string productCoefficients)
    {
        var balanced = Balance(reactants, products);

        balanced.ReactantCoefficients.ShouldBe(Numbers(reactantCoefficients));
        balanced.ProductCoefficients.ShouldBe(Numbers(productCoefficients));
    }

    [Theory]
    [InlineData("Ag^+ Cl^-", "AgCl", "1 1", "1")]
    [InlineData("MnO4^- Fe^2+ H^+", "Mn^2+ Fe^3+ H2O", "1 5 8", "1 5 4")]
    [InlineData("Cr2O7^2- H^+ I^-", "Cr^3+ I2 H2O", "1 14 6", "2 3 7")]
    public void TryBalance_BalancesIonicEquationsUsingCharge(
        string reactants, string products, string reactantCoefficients, string productCoefficients)
    {
        var balanced = Balance(reactants, products);

        balanced.ReactantCoefficients.ShouldBe(Numbers(reactantCoefficients));
        balanced.ProductCoefficients.ShouldBe(Numbers(productCoefficients));
    }

    [Theory]
    [InlineData("Fe^2+", "Fe^3+ e-", "1", "1 1")]
    [InlineData("MnO4^- H^+ e-", "Mn^2+ H2O", "1 8 5", "1 4")]
    [InlineData("H2O", "O2 H^+ e-", "2", "1 4 4")]
    public void TryBalance_BalancesHalfEquationsWithElectrons(
        string reactants, string products, string reactantCoefficients, string productCoefficients)
    {
        var balanced = Balance(reactants, products);

        balanced.ReactantCoefficients.ShouldBe(Numbers(reactantCoefficients));
        balanced.ProductCoefficients.ShouldBe(Numbers(productCoefficients));
    }

    [Theory]
    [InlineData("H2", "O2")]
    [InlineData("Na Cl2", "NaCl K")]
    [InlineData("H2 O2 N2", "H2O")]
    [InlineData("Fe^2+", "Fe^3+")]
    public void TryBalance_RefusesAnEquationThatCannotBalance(string reactants, string products) =>
        Refuse(reactants, products).ShouldBe(BalanceError.Unbalanceable);

    [Theory]
    [InlineData("C O2", "CO CO2")]
    [InlineData("C7H6O3 C4H6O3", "C9H8O4 C2H4O2")]
    public void TryBalance_RefusesAnUnderDeterminedEquation(string reactants, string products) =>
        Refuse(reactants, products).ShouldBe(BalanceError.UnderDetermined);

    [Theory]
    [InlineData("", "H2O")]
    [InlineData("H2 O2", "")]
    public void TryBalance_RefusesAnEmptySide(string reactants, string products) =>
        Refuse(reactants, products).ShouldBe(BalanceError.EmptySide);

    private static BalancedEquation Balance(string reactants, string products)
    {
        EquationBalancer.TryBalance(Species(reactants), Species(products), out var balanced, out var error)
            .ShouldBeTrue($"'{reactants} -> {products}' was refused as {error}");

        return balanced;
    }

    private static BalanceError Refuse(string reactants, string products)
    {
        EquationBalancer.TryBalance(Species(reactants), Species(products), out _, out var error)
            .ShouldBeFalse();

        return error;
    }

    private static IReadOnlyList<ChemicalComposition> Species(string formulas) =>
    [
        .. formulas.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(formula =>
        {
            if (formula == "e-")
            {
                return EquationBalancer.Electron;
            }

            FormulaParser.TryParse(formula, out var composition, out var error)
                .ShouldBeTrue($"'{formula}' was refused as {error}");

            return composition;
        })
    ];

    private static int[] Numbers(string values) =>
    [
        ..values.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
    ];
}
