namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ConditionsRequest(
    decimal Kelvin,
    decimal Kilopascals,
    IReadOnlyList<ConditionsReactionEntry> Reactions,
    bool IncludeCurves = false);
