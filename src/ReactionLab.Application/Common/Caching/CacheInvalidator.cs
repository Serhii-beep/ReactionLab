using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Application.Common.Caching;

public sealed class CacheInvalidator(HybridCache cache, ILogger<CacheInvalidator> logger)
{
    public async Task InvalidateAsync(
        IReadOnlyCollection<string> keys,
        string tag,
        CancellationToken cancellationToken)
    {
        try
        {
            if (keys.Count > 0)
            {
                await cache.RemoveAsync(keys, cancellationToken);
            }

            await cache.RemoveByTagAsync(tag, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Cache invalidation failed for tag {Tag}.", tag);
        }
    }
}
