using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Substances.GetSubstanceById;

public sealed class GetSubstanceByIdHandler(IAppDbContext context, HybridCache cache, CatalogCacheScope scope)
    : IQueryHandler<GetSubstanceByIdQuery, SubstanceResponse>
{
    public async ValueTask<Result<SubstanceResponse>> HandleAsync(GetSubstanceByIdQuery query, CancellationToken cancellationToken)
    {
        var id = SubstanceId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Substance(scope.Name, query.Id, query.Locale),
            async token => await SubstanceQueries.FirstResponseAsync(
                context.Substances.AsNoTracking().Where(substance => substance.Id == id),
                query.Locale,
                token),
            CachePolicies.Catalog,
            [CacheTags.Substances],
            cancellationToken);

        return response is null ? SubstanceErrors.NotFound(query.Id) : response;
    }
}
