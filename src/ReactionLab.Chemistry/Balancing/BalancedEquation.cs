namespace ReactionLab.Chemistry.Balancing;

public sealed class BalancedEquation
{
    internal BalancedEquation(IReadOnlyList<int> reactants, IReadOnlyList<int> products)
    {
        ReactantCoefficients = reactants;
        ProductCoefficients = products;
    }

    public IReadOnlyList<int> ReactantCoefficients { get; }

    public IReadOnlyList<int> ProductCoefficients { get; }
}
