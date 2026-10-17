namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record BalanceResult(IReadOnlyList<int>? ReactantCoefficients, IReadOnlyList<int>? ProductCoefficients, string? Error, string? InvalidFormula);
