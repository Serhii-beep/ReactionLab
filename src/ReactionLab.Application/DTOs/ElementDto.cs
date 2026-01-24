using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public record ElementDto(
    Guid Id,
    int AtomicNumber,
    string Symbol,
    string Name,
    decimal AtomicMass,
    ElementCategory Category,
    int Period,
    int? Group,
    string? ElectronConfiguration,
    decimal? Electronegativity,
    decimal? AtomicRadius,
    decimal? IonizationEnergy,
    decimal? MeltingPoint,
    decimal? BoilingPoint,
    decimal? Density,
    string? Color,
    MatterState StateAtRoomTemp,
    string DisplayColor,
    decimal Radius3D,
    string? DiscoveryInfo,
    string? InterestingFacts
);

public record ElementSummaryDto(
    Guid Id,
    int AtomicNumber,
    string Symbol,
    string Name,
    decimal AtomicMass,
    ElementCategory Category,
    int Period,
    int? Group,
    MatterState StateAtRoomTemp,
    string DisplayColor
);

public record CreateElementDto(
    int AtomicNumber,
    string Symbol,
    string Name,
    decimal AtomicMass,
    ElementCategory Category,
    int Period,
    int? Group,
    string? ElectronConfiguration,
    decimal? Electronegativity,
    decimal? AtomicRadius,
    decimal? IonizationEnergy,
    decimal? MeltingPoint,
    decimal? BoilingPoint,
    decimal? Density,
    string? Color,
    MatterState StateAtRoomTemp,
    string DisplayColor,
    decimal Radius3D,
    string? DiscoveryInfo,
    string? InterestingFacts
);

public record UpdateElementDto(
    string Name,
    decimal AtomicMass,
    ElementCategory Category,
    string? ElectronConfiguration,
    decimal? Electronegativity,
    decimal? AtomicRadius,
    decimal? IonizationEnergy,
    decimal? MeltingPoint,
    decimal? BoilingPoint,
    decimal? Density,
    string? Color,
    MatterState StateAtRoomTemp,
    string DisplayColor,
    decimal Radius3D,
    string? DiscoveryInfo,
    string? InterestingFacts
);