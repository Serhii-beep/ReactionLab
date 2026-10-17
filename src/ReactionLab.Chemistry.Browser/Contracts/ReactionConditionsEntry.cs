namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ReactionConditionsEntry(
    KineticsEntry? Kinetics,
    EquilibriumEntry? Equilibrium,
    ConditionsCurveEntry? Curve,
    IReadOnlyList<string> SpeciesWithoutEntropy);
