namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record PredictionResult(
    IReadOnlyList<PredictedReactionEntry>? Reactions,
    string? Error,
    string? InvalidFormula);
