using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Reactions;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed class GetReleaseReactionHandler(IReleaseReadContext release, HybridCache cache)
    : IQueryHandler<GetReleaseReactionQuery, ReactionResponse>
{
    public async ValueTask<Result<ReactionResponse>> HandleAsync(GetReleaseReactionQuery query, CancellationToken cancellationToken)
    {
        release.UseRelease(query.Release.ReleaseId);

        var id = ReactionId.From(query.Id);
        var reaction = await cache.GetOrCreateAsync(
            CacheKeys.ReleaseReaction(query.Release.ReleaseId, query.Release.Locale, query.Id),
            async token => await ReactionQueries.FirstResponseAsync(
                release.Reactions.Where(candidate => candidate.Id == id),
                release.Substances,
                query.Release.Locale,
                token),
            CachePolicies.Release,
            cancellationToken: cancellationToken);

        return reaction is null
            ? Result.Failure<ReactionResponse>(ReactionErrors.NotFound(query.Id))
            : Result.Success(reaction);
    }
}
