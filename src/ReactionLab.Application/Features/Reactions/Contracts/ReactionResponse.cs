using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Reactions.Contracts;

public sealed record ReactionResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Mechanism,
    string? SafetyWarnings,
    IReadOnlyList<string> RealWorldExamples,
    ReactionType Type,
    int Difficulty,
    bool IsReversible,
    IReadOnlyList<ReactionparticipantResponse> Participants,
    decimal? EnthalpyKilojoulesPerMole,
    decimal? ActivationEnergyKilojoulesPerMole,
    decimal? ReverseActivationEnergyKilojoulesPerMole,
    bool? IsExothermic,
    decimal? TemperatureKelvin,
    decimal? PressureKilopascals,
    string? Catalyst,
    string? EffectPresetKey,
    int? AnimationDurationMilliseconds,
    IReadOnlyList<string> Tags,
    string Rule,
    decimal Confidence);
