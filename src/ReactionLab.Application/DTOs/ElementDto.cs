using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public class ElementDto
{
    public Guid Id { get; init; }

    public int AtomicNumber { get; init; }

    public string Symbol { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal AtomicMass { get; init; }

    public ElementCategory Category { get; init; }

    public int Period { get; init; }

    public int? Group { get; init; }

    public string? ElectronConfiguration { get; init; }

    public decimal? Electronegativity { get; init; }

    public decimal? AtomicRadius { get; init; }

    public decimal? IonizationEnergy { get; init; }

    public decimal? MeltingPoint { get; init; }

    public decimal? BoilingPoint { get; init; }

    public decimal? Density { get; init; }

    public string? Color { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string DisplayColor { get; init; } = default!;

    public decimal Radius3D { get; init; }

    public string? DiscoveryInfo { get; init; }

    public string? InterestingFacts { get; init; }
}

public class ElementSummaryDto
{
    public Guid Id { get; init; }

    public int AtomicNumber { get; init; }

    public string Symbol { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal AtomicMass { get; init; }

    public ElementCategory Category { get; init; }

    public int Period { get; init; }

    public int? Group { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string DisplayColor { get; init; } = default!;
}

public class CreateElementDto
{
    public int AtomicNumber { get; init; }

    public string Symbol { get; init; } = default!;

    public string Name { get; init; } = default!;

    public decimal AtomicMass { get; init; }

    public ElementCategory Category { get; init; }

    public int Period { get; init; }

    public int? Group { get; init; }

    public string? ElectronConfiguration { get; init; }

    public decimal? Electronegativity { get; init; }

    public decimal? AtomicRadius { get; init; }

    public decimal? IonizationEnergy { get; init; }

    public decimal? MeltingPoint { get; init; }

    public decimal? BoilingPoint { get; init; }

    public decimal? Density { get; init; }

    public string? Color { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string DisplayColor { get; init; } = default!;

    public decimal Radius3D { get; init; }

    public string? DiscoveryInfo { get; init; }

    public string? InterestingFacts { get; init; }
}

public class UpdateElementDto
{
    public string Name { get; init; } = default!;

    public decimal AtomicMass { get; init; }

    public ElementCategory Category { get; init; }

    public int Period { get; init; }

    public int? Group { get; init; }

    public string? ElectronConfiguration { get; init; }

    public decimal? Electronegativity { get; init; }

    public decimal? AtomicRadius { get; init; }

    public decimal? IonizationEnergy { get; init; }

    public decimal? MeltingPoint { get; init; }

    public decimal? BoilingPoint { get; init; }

    public decimal? Density { get; init; }

    public string? Color { get; init; }

    public MatterState StateAtRoomTemp { get; init; }

    public string DisplayColor { get; init; } = default!;

    public decimal Radius3D { get; init; }

    public string? DiscoveryInfo { get; init; }

    public string? InterestingFacts { get; init; }
}
