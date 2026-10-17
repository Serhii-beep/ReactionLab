using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Application.Features.Substances.Contracts;

public sealed record SubstanceSummaryResponse(
    Guid Id,
    string Formula,
    string Name,
    SubstanceKind Kind,
    bool IsOrganic,
    MatterState StateAtRoomTemperature,
    decimal? WeightGramsPerMole,
    string? Category);
