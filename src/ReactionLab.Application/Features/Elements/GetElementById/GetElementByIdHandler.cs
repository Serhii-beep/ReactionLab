using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.GetElementById;

public sealed class GetElementByIdHandler(IAppDbContext context, HybridCache cache)
    : IQueryHandler<GetElementByIdQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(
        GetElementByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = ElementId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Element(query.Id, query.Locale),
            async token => await ElementQueries.FirstResponseAsync(
                context.Elements.AsNoTracking().Where(element => element.Id == id),
                query.Locale,
                token),
            CachePolicies.Reference,
            [CacheTags.Elements],
            cancellationToken);

        return response is null ? ElementErrors.NotFound(query.Id) : response;
    }
}
