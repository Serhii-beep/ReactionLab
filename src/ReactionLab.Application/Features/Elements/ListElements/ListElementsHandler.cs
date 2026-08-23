using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Features.Elements.ListElements;

public sealed class ListElementsHandler(IAppDbContext context, ICatalogSearch search)
    : IQueryHandler<ListElementsQuery, IReadOnlyList<ElementSummaryResponse>>
{
    public async ValueTask<Result<IReadOnlyList<ElementSummaryResponse>>> HandleAsync(ListElementsQuery query, CancellationToken cancellationToken)
    {
        var elements = context.Elements.AsNoTracking();

        var ordered = string.IsNullOrWhiteSpace(query.Search)
            ? elements.OrderBy(element => element.AtomicNumber)
            : search.Matching(elements, query.Search.Trim())
                .ThenBy(element => element.AtomicNumber);

        return Result.Success(
            await ElementQueries.SummariesAsync(ordered, query.Locale, cancellationToken));
    }
}
