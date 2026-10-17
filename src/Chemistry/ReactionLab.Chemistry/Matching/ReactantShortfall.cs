namespace ReactionLab.Chemistry.Matching;

public readonly record struct ReactantShortfall<TKey>(TKey Substance, int Missing)
    where TKey : notnull;
