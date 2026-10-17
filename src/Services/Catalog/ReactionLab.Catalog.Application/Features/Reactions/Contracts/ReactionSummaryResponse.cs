using ReactionLab.Catalog.Domain.Enums;

namespace ReactionLab.Catalog.Application.Features.Reactions.Contracts;

public sealed record ReactionSummaryResponse(
    Guid Id,
    string Name,
    ReactionType Type,
    int Difficulty,
    bool IsReversible,
    decimal? EnthalpyKilojoulesPerMole,
    decimal? ActivationEnergyKilojoulesPerMole,
    bool? IsExothermic,
    string? EffectPresetKey,
    int? AnimationDurationMilliseconds,
    decimal? TemperatureKelvin,
    decimal? PressureKilopascals,
    string? Catalyst,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ReactionParticipantResponse> Participants);
