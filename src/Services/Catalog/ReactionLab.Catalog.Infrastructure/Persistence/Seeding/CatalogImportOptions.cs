using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

public sealed class CatalogImportOptions
{
    public const string SectionName = "CatalogImport";

    public string Build { get; init; } = CuratedCatalogBuild.BuildId;

    public bool AllowMassRetirement { get; init; }
}
