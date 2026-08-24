using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.ListSubstances;

public sealed class ListSubstancesHandler(IAppDbContext context, ICatalogSearch search)
    : IQueryHandler<ListSubstancesQuery, CursorPagedResult<SubstanceSummaryResponse>>
{
    private const int MaximumSearchDepth = 1000;

    public async ValueTask<Result<CursorPagedResult<SubstanceSummaryResponse>>> HandleAsync(ListSubstancesQuery query, CancellationToken cancellationToken)
    {
        Cursor? cursor = null;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var decoded = Cursor.Decode(query.Page.Cursor);
            if (decoded.IsFailure)
            {
                return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(decoded.Error);
            }

            cursor = decoded.Value;
        }

        var substances = context.Substances.AsNoTracking();

        return string.IsNullOrWhiteSpace(query.Search)
            ? await BrowseAsync(substances, cursor, query, cancellationToken)
            : await SearchAsync(substances, cursor, query, cancellationToken);
    }

    private static async Task<Result<CursorPagedResult<SubstanceSummaryResponse>>> BrowseAsync(
        IQueryable<Substance> substances,
        Cursor? cursor,
        ListSubstancesQuery query,
        CancellationToken cancellationToken)
    {
        if (cursor is not null)
        {
            if (!cursor.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(Cursor.Malformed);
            }

            var after = SubstanceId.From(cursor.AfterId);
            substances = substances.Where(substance => substance.Id > after);
        }

        var rows = await SubstanceQueries.SummariesAsync(
            substances.OrderBy(substance => substance.Id).Take(query.Page.Limit + 1),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, last => Cursor.After(last.Id));
    }

    private async Task<Result<CursorPagedResult<SubstanceSummaryResponse>>> SearchAsync(
        IQueryable<Substance> substances,
        Cursor? cursor,
        ListSubstancesQuery query,
        CancellationToken cancellationToken)
    {
        var skip = 0;

        if (cursor is not null)
        {
            if (cursor.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(Cursor.Malformed);
            }

            skip = cursor.Skip;
        }

        if (skip >= MaximumSearchDepth)
        {
            return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(SubstanceErrors.SearchTooDeep(MaximumSearchDepth));
        }

        var rows = await SubstanceQueries.SummariesAsync(
            search.Matching(substances, query.Search!.Trim())
                .ThenBy(substance => substance.Id)
                .Skip(skip)
                .Take(query.Page.Limit + 1),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, _ => Cursor.Skipping(skip + query.Page.Limit));
    }

    private static Result<CursorPagedResult<SubstanceSummaryResponse>> Page(
        IReadOnlyList<SubstanceSummaryResponse> rows,
        int pageSize,
        Func<SubstanceSummaryResponse, Cursor> next)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.Take(pageSize).ToList() : rows;

        return Result.Success(new CursorPagedResult<SubstanceSummaryResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? next(items[^1]).Encode() : null
        });
    }
}
