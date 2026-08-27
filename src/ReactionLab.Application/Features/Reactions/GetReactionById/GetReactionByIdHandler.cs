using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Application.Features.Reactions.GetReactionById;

public sealed class GetReactionByIdHandler(IAppDbContext context, HybridCache cache)
    : IQueryHandler<GetReactionByIdQuery, ReactionResponse>
{
    public async ValueTask<Result<ReactionResponse>> HandleAsync(GetReactionByIdQuery query, CancellationToken cancellationToken)
    {
        var id = ReactionId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Reaction(query.Id, query.Locale),
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
