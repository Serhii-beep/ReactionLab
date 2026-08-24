using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.Contracts;

public sealed record SubstanceSummaryResponse(
    Guid Id,
    string Formula,
    string Name,
    SubstanceKind Kind,
    bool IsOrganic,
    MatterState StateAtRoomTemperature,
    decimal? WeightGramsPerMole,
    string? Category);
