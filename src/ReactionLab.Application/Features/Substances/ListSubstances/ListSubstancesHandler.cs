using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.ListSubstances;

public sealed class ListSubstancesHandler(IAppDbContext context, ICatalogSearch search, HybridCache cache)
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

        var browsing = string.IsNullOrWhiteSpace(query.Search);

        if (cursor is not null && cursor.IsKeyset != browsing)
        {
            return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(Cursor.Malformed);
        }

        if (!browsing && cursor is { Skip: >= MaximumSearchDepth })
        {
            return Result.Failure<CursorPagedResult<SubstanceSummaryResponse>>(
                SubstanceErrors.SearchTooDeep(MaximumSearchDepth));
        }

        var page = await cache.GetOrCreateAsync(
            CacheKeys.SubstanceList(query.Search, query.Page.Cursor, query.Page.Limit, query.Locale),
            async token => browsing
                ? await BrowseAsync(cursor, query, token)
                : await SearchAsync(cursor, query, token),
            browsing ? CachePolicies.Catalog : CachePolicies.Query,
            [CacheTags.Substances],
            cancellationToken);

        return Result.Success(page);
    }

    private async Task<CursorPagedResult<SubstanceSummaryResponse>> BrowseAsync(
        Cursor? cursor,
        ListSubstancesQuery query,
        CancellationToken cancellationToken)
    {
        var substances = context.Substances.AsNoTracking();
        if (cursor is not null)
        {
            var after = SubstanceId.From(cursor.AfterId);
            substances = substances.Where(substance => substance.Id > after);
        }

        var rows = await SubstanceQueries.SummariesAsync(
            substances.OrderBy(substance => substance.Id).Take(query.Page.Limit + 1),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, last => Cursor.After(last.Id));
    }

    private async Task<CursorPagedResult<SubstanceSummaryResponse>> SearchAsync(
        Cursor? cursor,
        ListSubstancesQuery query,
        CancellationToken cancellationToken)
    {
        var skip = cursor?.Skip ?? 0;

        var rows = await SubstanceQueries.SummariesAsync(
            search.Matching(context.Substances.AsNoTracking(), query.Search!.Trim())
                .ThenBy(substance => substance.Id)
                .Skip(skip)
                .Take(query.Page.Limit + 1),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, _ => Cursor.Skipping(skip + query.Page.Limit));
    }

    private static CursorPagedResult<SubstanceSummaryResponse> Page(
        IReadOnlyList<SubstanceSummaryResponse> rows,
        int pageSize,
        Func<SubstanceSummaryResponse, Cursor> next)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.Take(pageSize).ToList() : rows;

        return new CursorPagedResult<SubstanceSummaryResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? next(items[^1]).Encode() : null
        };
    }
}
