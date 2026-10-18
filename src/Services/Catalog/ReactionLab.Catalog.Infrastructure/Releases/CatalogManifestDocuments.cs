namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed record CatalogManifestDocuments(IReadOnlyDictionary<string, string> Elements, string Reference);
