using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.Contracts;

public sealed record SubstanceResponse(
    Guid Id,
    string Formula,
    string HillFormula,
    string Name,
    string? IupacName,
    string? Description,
    string? SafetyInformation,
    IReadOnlyList<string> CommonNames,
    IReadOnlyList<string> Uses,
    IReadOnlyList<string> InterestingFacts,
    SubstanceKind Kind,
    bool IsOrganic,
    MatterState StateAtRoomTemperature,
    decimal? WeightGramsPerMole,
    string? Category,
    MolecularStructureResponse? Structure);
