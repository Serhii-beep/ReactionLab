using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Quantities;

namespace ReactionLab.Chemistry.Stoichiometry;

public sealed class ReactionOutcome
{
    private ReactionOutcome(
        int? limitingReactantIndex,
        Amount extent,
        IReadOnlyList<Amount> productAmounts,
        IReadOnlyList<Amount> excessReactants)
    {
        LimitingReactantIndex = limitingReactantIndex;
        Extent = extent;
        ProductAmounts = productAmounts;
        ExcessReactants = excessReactants;
    }

    public int? LimitingReactantIndex { get; }

    public Amount Extent { get; }

    public IReadOnlyList<Amount> ProductAmounts { get; }

    public IReadOnlyList<Amount> ExcessReactants { get; }

    public static ReactionOutcome For(IReadOnlyList<Amount> available, BalancedEquation equation)
    {
        if (available.Count != equation.ReactantCoefficients.Count)
        {
            throw new ArgumentException("One amount is required per reactant.", nameof(available));
        }

        var extent = decimal.MaxValue;
        int? limiting = null;

        for (var i = 0; i < available.Count; i++)
        {
            var possible = available[i].Moles / equation.ReactantCoefficients[i];

            if (possible < extent)
            {
                extent = possible;
                limiting = i;
            }
            else if (possible == extent)
            {
                limiting = null;
            }
        }

        return new ReactionOutcome(
            limiting,
            Amount.FromMoles(extent),
            [.. equation.ProductCoefficients.Select(
                coefficient => Amount.FromMoles(extent * coefficient))],
            [.. available.Select((amount, index) => Amount.FromMoles(
                Math.Max(0m, amount.Moles - extent * equation.ReactantCoefficients[index])))]);
    }
}
