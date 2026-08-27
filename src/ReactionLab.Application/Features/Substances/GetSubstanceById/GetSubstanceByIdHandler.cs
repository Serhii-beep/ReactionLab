using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.GetSubstanceById;

public sealed class GetSubstanceByIdHandler(IAppDbContext context, HybridCache cache)
    : IQueryHandler<GetSubstanceByIdQuery, SubstanceResponse>
{
    public async ValueTask<Result<SubstanceResponse>> HandleAsync(GetSubstanceByIdQuery query, CancellationToken cancellationToken)
    {
        var id = SubstanceId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Substance(query.Id, query.Locale),
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
