using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record PredictionRequest(
    IReadOnlyList<string> Formulas,
    IReadOnlyDictionary<string, Phase>? StandardStates = null);
