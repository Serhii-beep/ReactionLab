namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed record CatalogManifest(string Version, IReadOnlyDictionary<string, string> Documents);
