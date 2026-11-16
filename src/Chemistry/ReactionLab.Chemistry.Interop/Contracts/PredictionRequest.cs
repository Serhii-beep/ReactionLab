using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record PredictionRequest(
    IReadOnlyList<string> Formulas,
    IReadOnlyDictionary<string, Phase>? StandardStates = null);
