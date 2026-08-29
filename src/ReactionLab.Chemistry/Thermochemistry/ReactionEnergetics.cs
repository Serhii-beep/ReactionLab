using ReactionLab.Chemistry.Balancing;

namespace ReactionLab.Chemistry.Thermochemistry;

public static class ReactionEnergetics
{
    public static decimal EnthalpyOfReaction(
        BalancedEquation equation,
        IReadOnlyList<StandardState> reactants,
        IReadOnlyList<StandardState> products)
    {
        RequireOneStatePerSpecies(equation, reactants, products);

        return Sum(equation.ProductCoefficients, products, state => state.FormationEnthalpyKjPerMol)
            - Sum(equation.ReactantCoefficients, reactants, state => state.FormationEnthalpyKjPerMol);
    }

    public static bool TryEntropyOfReaction(
        BalancedEquation equation,
        IReadOnlyList<StandardState> reactants,
        IReadOnlyList<StandardState> products,
        out decimal joulesPerMoleKelvin)
    {
        RequireOneStatePerSpecies(equation, reactants, products);
        joulesPerMoleKelvin = 0m;

        if (reactants.Concat(products).Any(state => state.StandardEntropyJPerMolKelvin is null))
        {
            return false;
        }

        joulesPerMoleKelvin = Sum(equation.ProductCoefficients, products, state => state.StandardEntropyJPerMolKelvin!.Value)
            - Sum(equation.ReactantCoefficients, reactants, state => state.StandardEntropyJPerMolKelvin!.Value);

        return true;
    }

    public static decimal GibbsFreeEnergy(
        decimal enthalpyKjPerMol,
        decimal entropyJPerMolKelvin,
        decimal kelvin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);

        return enthalpyKjPerMol - kelvin * entropyJPerMolKelvin / 1000m;
    }

    public static decimal EstimateActivationEnergy(
        decimal enthalpyKjPerMol,
        decimal intrinsicBarrierKjPerMol,
        decimal transferCoefficient)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(intrinsicBarrierKjPerMol);
        ArgumentOutOfRangeException.ThrowIfNegative(transferCoefficient);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(transferCoefficient, 1m);

        var estimate = intrinsicBarrierKjPerMol + transferCoefficient * enthalpyKjPerMol;

        return Math.Max(estimate, Math.Max(0m, enthalpyKjPerMol));
    }

    private static void RequireOneStatePerSpecies(
        BalancedEquation equation,
        IReadOnlyList<StandardState> reactants,
        IReadOnlyList<StandardState> products)
    {
        if (reactants.Count != equation.ReactantCoefficients.Count
            || products.Count != equation.ProductCoefficients.Count)
        {
            throw new ArgumentException(
                "One standard state is required per species.", nameof(equation));
        }
    }

    private static decimal Sum(
        IReadOnlyList<int> coefficients,
        IReadOnlyList<StandardState> states,
        Func<StandardState, decimal> value)
    {
        var total = 0m;

        for (var i = 0; i < states.Count; i++)
        {
            total += coefficients[i] * value(states[i]);
        }

        return total;
    }
}
