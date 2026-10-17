namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record IonBehaviorsEntry(
    IReadOnlyList<string> ThermallyStableCations,
    IReadOnlyList<string> OxidizingAnions,
    IReadOnlyList<string> UnstableAcidAnions,
    IReadOnlyList<string> HydrolyzingAnions);
