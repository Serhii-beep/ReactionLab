using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Elements.Contracts;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.GetElementById;

public sealed class GetElementByIdHandler(IAppDbContext context, HybridCache cache, CatalogCacheScope scope)
    : IQueryHandler<GetElementByIdQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(
        GetElementByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = ElementId.From(query.Id);

        var response = await cache.GetOrCreateAsync(
            CacheKeys.Element(scope.Name, query.Id, query.Locale),
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
