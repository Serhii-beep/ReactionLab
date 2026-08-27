using Microsoft.Extensions.Caching.Hybrid;

namespace ReactionLab.Application.Common.Caching;

public static class CachePolicies
{
    public static readonly HybridCacheEntryOptions Reference = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public static readonly HybridCacheEntryOptions Catalog = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    public static readonly HybridCacheEntryOptions Query = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)
    };
}
