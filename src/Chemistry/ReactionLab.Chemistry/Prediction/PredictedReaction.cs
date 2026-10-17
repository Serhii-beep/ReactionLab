namespace ReactionLab.Chemistry.Prediction;

public sealed class PredictedReaction
{
    public PredictedReaction(
        IReadOnlyList<int> reactants,
        IReadOnlyList<string> products,
        string rule,
        decimal confidence,
        decimal? minimumKelvin = null)
    {
        if (reactants.Count == 0)
        {
            throw new ArgumentException("A prediction consumes at least one reactant.", nameof(reactants));
        }

        if (products.Count == 0)
        {
            throw new ArgumentException("A prediction yields at least one product.", nameof(products));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        ArgumentOutOfRangeException.ThrowIfNegative(confidence);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(confidence, 1m);

        Reactants = reactants;
        Products = products;
        Rule = rule;
        Confidence = confidence;
        MinimumKelvin = minimumKelvin;
    }

    public IReadOnlyList<int> Reactants { get; }

    public IReadOnlyList<string> Products { get; }

    public string Rule { get; }

    public decimal Confidence { get; }

    public decimal? MinimumKelvin { get; }
}
