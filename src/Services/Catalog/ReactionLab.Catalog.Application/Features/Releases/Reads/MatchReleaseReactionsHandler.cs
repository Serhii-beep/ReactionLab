using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Common.Pagination;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed class MatchReleaseReactionsHandler(ICatalogReleases releases, IReleaseReadContext release, HybridCache cache)
    : IQueryHandler<MatchReleaseReactionsQuery, CursorPagedResult<ReactionSummaryResponse>>
{
    public const int PageSize = 30;

    public static bool IsCanonical(IReadOnlyList<Guid> available)
    {
        for (var index = 1; index < available.Count; index++)
        {
            if (string.CompareOrdinal(available[index - 1].ToString("D"), available[index].ToString("D")) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    public async ValueTask<Result<CursorPagedResult<ReactionSummaryResponse>>> HandleAsync(
        MatchReleaseReactionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Available.Count is 0 or > ReleaseErrors.MaximumBench)
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(ReleaseErrors.BenchSize);
        }

        if (!IsCanonical(query.Available))
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(ReleaseErrors.BenchNotCanonical);
        }

        if (releases.Classify(query.Release.ReleaseId) != ReleaseAvailability.Readable)
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(ReleaseErrors.NotHeld);
        }

        release.UseRelease(query.Release.ReleaseId);

        var page = await cache.GetOrCreateAsync(
            CacheKeys.ReleaseMatch(query.Release.ReleaseId, query.Release.Locale, query.Available, query.Match),
            async token =>
            {
                var matched = releases.Match(query.Release.ReleaseId, [.. query.Available], query.Match, PageSize)
                    ?? throw new InvalidOperationException($"Release {query.Release.ReleaseId} was dropped while it was being matched.");

                return new CursorPagedResult<ReactionSummaryResponse>
                {
                    Items = await ReleaseMatching.SummariesAsync(release, matched, query.Release.Locale, token),
                    PageSize = PageSize,
                    HasMore = false,
                    NextCursor = null
                };
            },
            CachePolicies.Release,
            cancellationToken: cancellationToken);

        return Result.Success(page);
    }
}
