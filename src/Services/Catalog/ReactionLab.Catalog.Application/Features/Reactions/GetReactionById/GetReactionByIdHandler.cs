using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Reactions.GetReactionById;

public sealed class GetReactionByIdHandler(IAppDbContext context, HybridCache cache, CatalogCacheScope scope)
    : IQueryHandler<GetReactionByIdQuery, ReactionResponse>
{
    public async ValueTask<Result<ReactionResponse>> HandleAsync(GetReactionByIdQuery query, CancellationToken cancellationToken)
    {
        var id = ReactionId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Reaction(scope.Name, query.Id, query.Locale),
            async token => await ReactionQueries.FirstResponseAsync(
                context.Reactions.AsNoTracking().Where(reaction => reaction.Id == id),
                context.Substances.AsNoTracking(),
                query.Locale,
                token),
            CachePolicies.Catalog,
            [CacheTags.Reactions],
            cancellationToken);

        return response is null ? ReactionErrors.NotFound(query.Id) : response;
    }
}
