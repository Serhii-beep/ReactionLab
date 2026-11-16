namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record KineticsEntry(
    double Log10RateFactor,
    double Log10ShareOverBarrier,
    double? Log10CollisionFactor);
