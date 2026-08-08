using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class Molecule : BaseEntity
{
    public string Formula { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? IUPACName { get; set; }

    public string? CommonNames { get; set; } // JSON array

    public decimal? MolecularWeight { get; set; }

    public string? Structure3D { get; set; } // JSON: atom positions, bonds

    public bool IsOrganic { get; set; }

    public string? Category { get; set; }

    public MatterState StateAtRoomTemp { get; set; }

    // Educational Content
    public string? Description { get; set; }

    public string? Uses { get; set; } // JSON array

    public string? SafetyInfo { get; set; }

    public string? InterestingFacts { get; set; } // JSON array

    public string? ImageUrl { get; set; }

    public string? Model3DUrl { get; set; }

    public ICollection<MoleculeElement> MoleculeElements { get; set; } = [];

    public ICollection<ReactionParticipant> ReactionParticipants { get; set; } = [];
}
