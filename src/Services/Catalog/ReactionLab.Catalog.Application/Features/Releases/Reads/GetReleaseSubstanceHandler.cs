using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Substances;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed class GetReleaseSubstanceHandler(IReleaseReadContext release, HybridCache cache)
    : IQueryHandler<GetReleaseSubstanceQuery, SubstanceResponse>
{
    public async ValueTask<Result<SubstanceResponse>> HandleAsync(GetReleaseSubstanceQuery query, CancellationToken cancellationToken)
    {
        release.UseRelease(query.Release.ReleaseId);

        var id = SubstanceId.From(query.Id);
        var substance = await cache.GetOrCreateAsync(
            CacheKeys.ReleaseSubstance(query.Release.ReleaseId, query.Release.Locale, query.Id),
            async token => await SubstanceQueries.FirstResponseAsync(
                release.Substances.Where(candidate => candidate.Id == id),
                query.Release.Locale,
                token),
            CachePolicies.Release,
            cancellationToken: cancellationToken);

        return substance is null
            ? Result.Failure<SubstanceResponse>(SubstanceErrors.NotFound(query.Id))
            : Result.Success(substance);
    }
}
