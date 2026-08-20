using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Elements.Contracts;

public sealed record ElementResponse(
    Guid Id,
    int AtomicNumber,
    string Symbol,
    string Name,
    decimal Mass,
    ElementCategory Category,
    MatterState StateAtRoomTemperature,
    int Period,
    int? Group,
    string DisplayColor,
    decimal? Electronegativity,
    decimal? CovalentRadiusPicometers,
    decimal? VanDerWaalsRadiusPicometers,
    decimal? MeltingPointKelvin,
    decimal? BoilingPointKelvin,
    string? ElectronConfiguration,
    string? DiscoveryInfo,
    IReadOnlyList<string> InterestingFacts);
