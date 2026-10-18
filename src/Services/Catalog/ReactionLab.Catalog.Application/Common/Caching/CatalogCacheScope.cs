using ReactionLab.Catalog.Application.Common.Abstractions;

namespace ReactionLab.Catalog.Application.Common.Caching;

public sealed class CatalogCacheScope(ICatalogReleases releases)
{
    public string Name => releases.ActiveRelease is { } release ? release.ToString("N") : "unreleased";
}
