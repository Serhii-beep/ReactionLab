namespace ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

public sealed record SubstanceRecord
{
    public required string Formula { get; init; }

    public required string Kind { get; init; }

    public required bool IsOrganic { get; init; }

    public required string State { get; init; }

    public decimal? MolecularWeight { get; init; }

    public string? Category { get; init; }

    public StructureRecord? Structure { get; init; }

    public int? PubChemCid { get; init; }

    public required Dictionary<string, SubstanceText> Translations { get; init; }

    public sealed record SubstanceText(
        string Name,
        string? IupacName,
        string? Description,
        string? SafetyInformation,
        List<string>? CommonNames,
        List<string>? Uses,
        List<string>? InterestingFacts);

    public sealed record StructureRecord(List<AtomRecord> Atoms, List<BondRecord> Bonds);

    public sealed record AtomRecord(string Symbol, double X, double Y, double Z);

    public sealed record BondRecord(int From, int To, string Type);
}
