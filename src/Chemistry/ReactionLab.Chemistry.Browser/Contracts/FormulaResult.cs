namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record FormulaResult(string? Hill, int Charge, IReadOnlyDictionary<string, int>? CountBySymbol, string? Error);
