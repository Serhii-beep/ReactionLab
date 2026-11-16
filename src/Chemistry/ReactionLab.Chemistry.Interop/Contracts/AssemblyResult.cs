namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record AssemblyResult(IReadOnlyList<float>? PositionsAngstroms, string? Error);
