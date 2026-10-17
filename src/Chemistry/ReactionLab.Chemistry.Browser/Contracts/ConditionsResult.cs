namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ConditionsResult(
    decimal ThermalEnergyKilojoulesPerMole,
    IReadOnlyList<ReactionConditionsEntry> Reactions);
