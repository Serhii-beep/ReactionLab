namespace ReactionLab.Chemistry.Matching;

public static class ReactantMatching
{
    public static ReactionMatch<TKey> Match<TKey>(
        IReadOnlyList<ReactantRequirement<TKey>> required,
        IReadOnlyDictionary<TKey, int> available)
        where TKey : notnull
    {
        if (required.Count == 0)
        {
            throw new ArgumentException("A reaction has at least one reactant.", nameof(required));
        }

        var runs = int.MaxValue;
        var requiredUnits = 0;
        var presentUnits = 0;
        var shortfall = new List<ReactantShortfall<TKey>>();

        foreach (var (substance, coefficient) in required)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coefficient, nameof(required));

            var count = available.TryGetValue(substance, out var held) ? Math.Max(0, held) : 0;

            runs = Math.Min(runs, count / coefficient);
            requiredUnits += coefficient;
            presentUnits += Math.Min(count, coefficient);

            if (count < coefficient)
            {
                shortfall.Add(new ReactantShortfall<TKey>(substance, coefficient - count));
            }
        }

        return new ReactionMatch<TKey>(runs, (decimal)presentUnits / requiredUnits, shortfall);
    }
}
