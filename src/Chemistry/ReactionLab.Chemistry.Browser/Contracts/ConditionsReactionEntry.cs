namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record ConditionsReactionEntry(
    IReadOnlyList<ConditionsParticipantEntry> Participants,
    decimal? ActivationEnergyKilojoulesPerMole = null);
