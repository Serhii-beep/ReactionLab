namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record SymmetryRequest(IReadOnlyList<string> Symbols, IReadOnlyList<float> PositionsAngstroms);
