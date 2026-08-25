using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Reactions.Contracts;

public sealed record ReactionSummaryResponse(
    Guid Id,
    string Name,
    ReactionType Type,
    int Difficulty,
    bool IsReversible,
    decimal? EnthalpyKilojoulesPerMole,
    bool? IsExothermic,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ReactionparticipantResponse> Participants);
