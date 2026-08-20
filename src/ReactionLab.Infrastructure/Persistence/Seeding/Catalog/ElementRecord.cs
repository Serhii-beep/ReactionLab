namespace ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

public sealed class ElementRecord
{
    public required int AtomicNumber { get; init; }

    public required string Symbol { get; init; }

    public required decimal AtomicMass { get; init; }

    public required string Category { get; init; }

    public required int Period { get; init; }

    public int? Group { get; init; }

    public required string State { get; init; }

    public required string DisplayColor { get; init; }

    public string? ElectronConfiguration { get; init; }

    public decimal? Electronegativity { get; init; }

    public decimal? CovalentRadiusPm { get; init; }

    public decimal? VanDerWaalsRadiusPm { get; init; }

    public decimal? MeltingPointK { get; init; }

    public decimal? BoilingPointK { get; init; }

    public required Dictionary<string, ElementText> Translations { get; init; }

    public sealed record ElementText(string Name, string? DiscoveryInfo, List<string>? InterestingFacts);
}
