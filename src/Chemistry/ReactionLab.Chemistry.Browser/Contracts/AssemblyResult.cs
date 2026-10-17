namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record AssemblyResult(IReadOnlyList<float>? PositionsAngstroms, string? Error);
