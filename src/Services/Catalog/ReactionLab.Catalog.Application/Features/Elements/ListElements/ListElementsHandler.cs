using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Application.Features.Elements.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.ListElements;

public sealed class ListElementsHandler(IAppDbContext context, ICatalogSearch search, HybridCache cache, CatalogCacheScope scope)
    : IQueryHandler<ListElementsQuery, IReadOnlyList<ElementSummaryResponse>>
{
    public async ValueTask<Result<IReadOnlyList<ElementSummaryResponse>>> HandleAsync(ListElementsQuery query, CancellationToken cancellationToken)
    {
        var summaries = await cache.GetOrCreateAsync(
            CacheKeys.ElementList(scope.Name, query.Search, query.Locale),
            async token =>
            {
                var elements = context.Elements.AsNoTracking();

                var ordered = string.IsNullOrWhiteSpace(query.Search)
                    ? elements.OrderBy(element => element.AtomicNumber)
                    : search.Matching(elements, query.Search.Trim())
                        .ThenBy(element => element.AtomicNumber);

                return await ElementQueries.SummariesAsync(ordered, query.Locale, token);
            },
            string.IsNullOrWhiteSpace(query.Search) ? CachePolicies.Reference : CachePolicies.Query,
            [CacheTags.Elements],
            cancellationToken);

        return Result.Success(summaries);
    }
}
