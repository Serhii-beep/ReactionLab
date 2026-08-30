namespace ReactionLab.Chemistry.Matching;

public readonly record struct ReactantRequirement<TKey>(TKey Substance, int Coefficient)
    where TKey : notnull;
