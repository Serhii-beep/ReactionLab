namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed record CatalogManifest(Guid Release, string Version, CatalogManifestDocuments Documents);
