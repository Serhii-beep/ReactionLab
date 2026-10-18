using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Releases.Snapshots;
using ReactionLab.Catalog.Application.Features.Substances;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases.Reads;

public sealed class FindReleaseSubstancesByStructureHandler(IReleaseReadContext release, HybridCache cache)
    : IQueryHandler<FindReleaseSubstancesByStructureQuery, IReadOnlyList<SubstanceResponse>>
{
    public const int KeyLength = 32;

    private const int MaximumCandidates = 20;

    public async ValueTask<Result<IReadOnlyList<SubstanceResponse>>> HandleAsync(
        FindReleaseSubstancesByStructureQuery query,
        CancellationToken cancellationToken)
    {
        if (query.StructureKey is not { Length: KeyLength } key || !key.All(char.IsAsciiHexDigitLower))
        {
            return Result.Failure<IReadOnlyList<SubstanceResponse>>(ReleaseErrors.StructureKeyMalformed);
        }

        release.UseRelease(query.Release.ReleaseId);

        var substances = await cache.GetOrCreateAsync(
            CacheKeys.ReleaseStructure(query.Release.ReleaseId, query.Release.Locale, key),
            async token => await SubstanceQueries.ResponsesAsync(
                release.Substances
                    .Where(substance => substance.RetiredAt == null
                        && EF.Property<string?>(substance, ReleaseColumns.StructureKey) == key)
                    .OrderBy(substance => substance.Id)
                    .Take(MaximumCandidates),
                query.Release.Locale,
                token),
            CachePolicies.Release,
            cancellationToken: cancellationToken);

        return Result.Success(substances);
    }
}
