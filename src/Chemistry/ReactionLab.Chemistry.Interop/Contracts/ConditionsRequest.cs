namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ConditionsRequest(
    decimal Kelvin,
    decimal Kilopascals,
    IReadOnlyList<ConditionsReactionEntry> Reactions,
    bool IncludeCurves = false);
