using ReactionLab.Chemistry.Balancing;

namespace ReactionLab.Chemistry.Thermochemistry;

public sealed class EnergyEstimator(StandardStateTable states, IReadOnlyList<ActivationBarrier> barriers)
{
    public bool TryEstimate(
        string rule,
        BalancedEquation equation,
        PhaseAssignment phases,
        IReadOnlyList<string> reactants,
        IReadOnlyList<string> products,
        out decimal enthalpyKjPerMol,
        out decimal activationEnergyKjPerMol)
    {
        enthalpyKjPerMol = 0m;
        activationEnergyKjPerMol = 0m;

        if (!TryStates(reactants, phases.Reactants, out var left)
            || !TryStates(products, phases.Products, out var right))
        {
            return false;
        }

        enthalpyKjPerMol = decimal.Round(ReactionEnergetics.EnthalpyOfReaction(equation, left, right), 1);

        var family = rule.Split('.')[0];

        foreach (var barrier in barriers)
        {
            if (!string.Equals(barrier.Family, family, StringComparison.Ordinal))
            {
                continue;
            }

            activationEnergyKjPerMol = decimal.Round(
                Math.Max(
                    ReactionEnergetics.EstimateActivationEnergy(enthalpyKjPerMol, barrier.IntrinsicKjPerMol, barrier.TransferCoefficient),
                    barrier.MinimumKjPerMol),
                1);

            return true;
        }

        return false;
    }

    private bool TryStates(
        IReadOnlyList<string> species, IReadOnlyList<Phase> phases, out IReadOnlyList<StandardState> found)
    {
        var resolved = new StandardState[species.Count];

        for (var i = 0; i < species.Count; i++)
        {
            if (!states.TryFind(species[i], phases[i], out resolved[i]))
            {
                found = [];

                return false;
            }
        }

        found = resolved;

        return true;
    }
}
