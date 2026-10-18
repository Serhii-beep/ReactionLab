namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

public sealed record CatalogBuildManifest(
    string Id,
    string ContentHash,
    int Substances,
    int Reactions,
    IReadOnlyList<string> ElementShards,
    IReadOnlyList<string> SubstanceShards,
    IReadOnlyList<string> ReactionShards)
{
    public const string ContainerName = "catalog-builds";

    public const string FileName = "manifest.json";
}
