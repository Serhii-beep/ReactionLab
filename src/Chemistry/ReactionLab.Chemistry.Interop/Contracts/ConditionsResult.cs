namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ConditionsResult(
    decimal ThermalEnergyKilojoulesPerMole,
    IReadOnlyList<ReactionConditionsEntry> Reactions);
