namespace ReactionLab.Chemistry.Interop.Contracts;

internal sealed record ConditionsReactionEntry(
    IReadOnlyList<ConditionsParticipantEntry> Participants,
    decimal? ActivationEnergyKilojoulesPerMole = null);
