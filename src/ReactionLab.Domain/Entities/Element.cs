using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class Element : BaseEntity
{
    public int AtomicNumber { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal AtomicMass { get; set; }

    public ElementCategory Category { get; set; }

    public int Period { get; set; }

    public int? Group { get; set; }

    public string? ElectronConfiguration { get; set; }

    public decimal? Electronegativity { get; set; }

    public decimal? AtomicRadius { get; set; }

    public decimal? IonizationEnergy { get; set; }

    public decimal? MeltingPoint { get; set; }

    public decimal? BoilingPoint { get; set; }

    public decimal? Density { get; set; }

    public string? Color { get; set; }

    public MatterState StateAtRoomTemp { get; set; }

    // 3D Rendering Properties
    public string DisplayColor { get; set; } = "#FFFFFF";

    public decimal Radius3D { get; set; } = 1.0m;
    
    // Educational Content
    public string? DiscoveryInfo { get; set; }

    public string? InterestingFacts { get; set; } // JSON array stored as string

    // Native properties
    public ICollection<MoleculeElement> MoleculeElements { get; set; } = [];
}