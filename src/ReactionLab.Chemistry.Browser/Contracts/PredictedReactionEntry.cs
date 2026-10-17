namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record PredictedReactionEntry(
    IReadOnlyList<PredictedParticipantEntry> Reactants,
    IReadOnlyList<PredictedParticipantEntry> Products,
    string Rule,
    decimal Confidence,
    decimal? MinimumKelvin,
    decimal? EnthalpyKilojoulesPerMole,
    decimal? ActivationEnergyKilojoulesPerMole);
