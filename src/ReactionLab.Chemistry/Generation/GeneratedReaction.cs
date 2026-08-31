namespace ReactionLab.Chemistry.Generation;

public sealed class GeneratedReaction
{
    internal GeneratedReaction(
        IReadOnlyList<GeneratedParticipant> reactants,
        IReadOnlyList<GeneratedParticipant> products,
        string rule,
        decimal confidence,
        decimal? minimumKelvin)
    {
        Reactants = reactants;
        Products = products;
        Rule = rule;
        Confidence = confidence;
        MinimumKelvin = minimumKelvin;
        Signature = Canonical(reactants) + " -> " + Canonical(products);
    }

    public IReadOnlyList<GeneratedParticipant> Reactants { get; }

    public IReadOnlyList<GeneratedParticipant> Products { get; }

    public string Rule { get; }

    public decimal Confidence { get; }

    public decimal? MinimumKelvin { get; }

    public string Signature { get; }

    private static string Canonical(IReadOnlyList<GeneratedParticipant> side) =>
        string.Join(" + ", side.Select(p => p.Formula).Order(StringComparer.Ordinal));
}
