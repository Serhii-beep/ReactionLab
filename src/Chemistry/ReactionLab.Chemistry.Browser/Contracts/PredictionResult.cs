namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record PredictionResult(
    IReadOnlyList<PredictedReactionEntry>? Reactions,
    string? Error,
    string? InvalidFormula);
