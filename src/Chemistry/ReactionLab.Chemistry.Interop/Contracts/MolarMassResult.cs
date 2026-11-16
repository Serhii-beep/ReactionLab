namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record MolarMassResult(decimal? GramsPerMole, string? Error, string? UnknownSymbol);
