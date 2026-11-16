namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record SymmetryRequest(IReadOnlyList<string> Symbols, IReadOnlyList<float> PositionsAngstroms);
