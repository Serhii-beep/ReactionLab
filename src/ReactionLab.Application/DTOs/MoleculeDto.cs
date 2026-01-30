using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public class MoleculeDto
{
    public Guid Id { get; init; }

    public string Formula { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? IUPACName { get; init; }

    public string? CommonNames { get; init; }

    public decimal? MolecularWeight { get; init; }

    public string? Structure3D { get; init; }

    public bool IsOrganic { get; init; }

    public string? Category { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string? Description { get; init; }

    public string? Uses { get; init; }

    public string? SafetyInfo { get; init; }

    public string? InterestingFacts { get; init; }

    public string? ImageUrl { get; init; }

    public string? Model3DUrl { get; init; }

    public IReadOnlyList<MoleculeElementDto> Elements { get; init; } = [];
}

public class MoleculeSummaryDto
{
    public Guid Id { get; init; }

    public string Formula { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal? MolecularWeight { get; init; }

    public bool IsOrganic { get; init; }

    public string? Category { get; init; }

    public MatterState StateAtRoomTemp { get; init; }
}

public class MoleculeElementDto
{
    public Guid ElementId { get; init; }

    public string Symbol { get; init; } = default!;

    public string Name { get; init; } = default!;

    public int Count { get; init; }
}

public class CreateMoleculeDto
{
    public string Formula { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? IUPACName { get; init; }

    public string? CommonNames { get; init; }

    public decimal? MolecularWeight { get; init; }

    public string? Structure3D { get; init; }

    public bool IsOrganic { get; init; }

    public string? Category { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string? Description { get; init; }

    public string? Uses { get; init; }

    public string? SafetyInfo { get; init; }

    public string? InterestingFacts { get; init; }

    public string? ImageUrl { get; init; }

    public string? Model3DUrl { get; init; }

    public IReadOnlyList<CreateMoleculeElementDto>? Elements { get; init; }
}

public class CreateMoleculeElementDto
{
    public Guid ElementId { get; init; }

    public int Count { get; init; }
}

public class UpdateMoleculeDto
{
    public string Name { get; init; } = default!;

    public string? IUPACName { get; init; }

    public string? CommonNames { get; init; }

    public decimal? MolecularWeight { get; init; }

    public string? Structure3D { get; init; }

    public bool IsOrganic { get; init; }

    public string? Category { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string? Description { get; init; }

    public string? Uses { get; init; }

    public string? SafetyInfo { get; init; }

    public string? InterestingFacts { get; init; }

    public string? ImageUrl { get; init; }

    public string? Model3DUrl { get; init; }
}