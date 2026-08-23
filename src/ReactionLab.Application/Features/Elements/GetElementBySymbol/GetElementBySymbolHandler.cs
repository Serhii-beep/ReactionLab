using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.GetElementBySymbol;

public sealed class GetElementBySymbolHandler(IAppDbContext context)
    : IQueryHandler<GetElementBySymbolQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(GetElementBySymbolQuery query, CancellationToken cancellationToken)
    {
        if (ElementSymbol.Match(query.Symbol) is not { } symbol)
        {
            return ElementSymbol.Invalid.WithArgs(("symbol", query.Symbol));
        }

        var response = await ElementQueries.FirstResponseAsync(
            context.Elements.AsNoTracking().Where(element => element.Symbol == symbol),
            query.Locale,
            cancellationToken);

        return response is null ? ElementErrors.SymbolNotFound(query.Symbol) : response;
    }
}
