using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Reactions.Contracts;

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
    IReadOnlyList<string> Tags,
    IReadOnlyList<ReactionParticipantResponse> Participants);
