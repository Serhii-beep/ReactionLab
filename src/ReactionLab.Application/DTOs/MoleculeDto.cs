using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public record MoleculeDto(
    Guid Id,
    string Formula,
    string Name,
    string? IUPACName,
    string? CommonNames,
    decimal? MolecularWeight,
    string? Structure3D,
    bool IsOrganic,
    string? Category,
    MatterState StateAtRoomTemp,
    string? Description,
    string? Uses,
    string? SafetyInfo,
    string? InterestingFacts,
    string? ImageUrl,
    string? Model3DUrl,
    IReadOnlyList<MoleculeElementDto> Elements
);

public record MoleculeSummaryDto(
    Guid Id,
    string Formula,
    string Name,
    decimal? MolecularWeight,
    bool IsOrganic,
    string? Category,
    MatterState StateAtRoomTemp
);

public record MoleculeElementDto(
    Guid ElementId,
    string Symbol,
    string Name,
    int Count
);

public record CreateMoleculeDto(
    string Formula,
    string Name,
    string? IUPACName,
    string? CommonNames,
    decimal? MolecularWeight,
    string? Structure3D,
    bool IsOrganic,
    string? Category,
    MatterState StateAtRoomTemp,
    string? Description,
    string? Uses,
    string? SafetyInfo,
    string? InterestingFacts,
    string? ImageUrl,
    string? Model3DUrl,
    IReadOnlyList<CreateMoleculeElementDto>? Elements
);

public record CreateMoleculeElementDto(
    Guid ElementId,
    int Count
);

public record UpdateMoleculeDto(
    string Name,
    string? IUPACName,
    string? CommonNames,
    decimal? MolecularWeight,
    string? Structure3D,
    bool IsOrganic,
    string? Category,
    MatterState StateAtRoomTemp,
    string? Description,
    string? Uses,
    string? SafetyInfo,
    string? InterestingFacts,
    string? ImageUrl,
    string? Model3DUrl
);