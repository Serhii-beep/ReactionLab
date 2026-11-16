namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record FormulaResult(string? Hill, int Charge, IReadOnlyDictionary<string, int>? CountBySymbol, string? Error);
