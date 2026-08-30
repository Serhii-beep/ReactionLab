namespace ReactionLab.Chemistry.Ions;

public sealed class ActivitySeries(IReadOnlyList<ActiveMetal> metals)
{
    public IReadOnlyList<ActiveMetal> Metals { get; } = metals;

    public bool TryFind(string symbol, out ActiveMetal metal)
    {
        foreach (var candidate in Metals)
        {
            if (string.Equals(candidate.Symbol, symbol, StringComparison.Ordinal))
            {
                metal = candidate;
                return true;
            }
        }

        metal = default;
        return false;
    }

    public bool Displaces(string displacer, string displaced)
    {
        var first = RankOf(displacer);
        var second = RankOf(displaced);

        return first >= 0 && second >= 0 && first < second;
    }

    private int RankOf(string symbol)
    {
        for (var i = 0; i < Metals.Count; i++)
        {
            if (string.Equals(Metals[i].Symbol, symbol, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
