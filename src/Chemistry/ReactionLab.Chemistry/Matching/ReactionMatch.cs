namespace ReactionLab.Chemistry.Matching;

public sealed class ReactionMatch<TKey>
    where TKey : notnull
{
    internal ReactionMatch(int runs, decimal completeness, IReadOnlyList<ReactantShortfall<TKey>> shortfall)
    {
        Runs = runs;
        Completeness = completeness;
        Shortfall = shortfall;
    }

    public int Runs { get; }

    public decimal Completeness { get; }

    public IReadOnlyList<ReactantShortfall<TKey>> Shortfall { get; }
}
