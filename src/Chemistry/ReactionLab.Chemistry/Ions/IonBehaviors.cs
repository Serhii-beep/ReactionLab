namespace ReactionLab.Chemistry.Ions;

public sealed record IonBehaviors(
    IReadOnlyList<string> ThermallyStableCations,
    IReadOnlyList<string> OxidizingAnions,
    IReadOnlyList<string> UnstableAcidAnions,
    IReadOnlyList<string> HydrolyzingAnions)
{
    public static IonBehaviors None { get; } = new([], [], [], []);
}
