using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.GetElementBySymbol;

public sealed class GetElementBySymbolHandler(IAppDbContext context, HybridCache cache)
    : IQueryHandler<GetElementBySymbolQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(GetElementBySymbolQuery query, CancellationToken cancellationToken)
    {
        if (ElementSymbol.Match(query.Symbol) is not { } symbol)
        {
            return ElementSymbol.Invalid.WithArgs(("symbol", query.Symbol));
        }

        var response = await cache.GetOrCreateAsync(
            CacheKeys.ElementBySymbol(symbol.Value, query.Locale),
            async token => await ElementQueries.FirstResponseAsync(
                context.Elements.AsNoTracking().Where(element => element.Symbol == symbol),
                query.Locale,
                token),
            CachePolicies.Reference,
            [CacheTags.Elements],
            cancellationToken);

        return response is null ? ElementErrors.SymbolNotFound(query.Symbol) : response;
    }
}
