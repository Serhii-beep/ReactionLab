using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Prediction;
using Shouldly;

namespace ReactionLab.Chemistry.UnitTests;

internal static class Species
{
    public static IReadOnlyList<ChemicalComposition> Of(string formulas) =>
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

    public static BalancedEquation Balanced(string reactants, string products)
    {
        EquationBalancer.TryBalance(Of(reactants), Of(products), out var balanced, out var error)
            .ShouldBeTrue($"'{reactants} -> {products}' was refused as {error}");

        return balanced;
    }

    public static IReadOnlyList<Reagent> Reagents(string formulas) =>
    [
        .. formulas.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(formula =>
        {
            Reagent.TryCreate(formula, out var reagent).ShouldBeTrue($"'{formula}' did not parse");

            return reagent;
        })
    ];
}
