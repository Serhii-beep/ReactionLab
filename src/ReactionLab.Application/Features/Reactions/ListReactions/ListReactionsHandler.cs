using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Application.Features.Reactions.ListReactions;

public sealed class ListReactionsHandler(
    IAppDbContext context,
    ICatalogSearch search,
    IReactionMatching matching,
    HybridCache cache)
    : IQueryHandler<ListReactionsQuery, CursorPagedResult<ReactionSummaryResponse>>
{
    private const int MaximumSearchDepth = 1000;

    public async ValueTask<Result<CursorPagedResult<ReactionSummaryResponse>>> HandleAsync(ListReactionsQuery query, CancellationToken cancellationToken)
    {
        Cursor? cursor = null;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var decoded = Cursor.Decode(query.Page.Cursor);
            if (decoded.IsFailure)
            {
                return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(decoded.Error);
            }

            cursor = decoded.Value;
        }

        var browsing = string.IsNullOrWhiteSpace(query.Search);

        if (cursor is not null && cursor.IsKeyset != browsing)
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(Cursor.Malformed);
        }

        if (!browsing && cursor is { Skip: >= MaximumSearchDepth })
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(
                Error.Validation(
                    "Reaction.SearchTooDeep",
                    "Search results are limited.",
                    field: "Cursor")
                .WithArgs(("max", MaximumSearchDepth)));
        }

        var page = await cache.GetOrCreateAsync(
            CacheKeys.ReactionList(
                query.Search, query.AvailableSubstanceIds, query.Match, query.Page.Cursor, query.Page.Limit, query.Locale),
            async token => browsing
                ? await BrowseAsync(cursor, query, token)
                : await SearchAsync(cursor, query, token),
            browsing && query.AvailableSubstanceIds is null ? CachePolicies.Catalog : CachePolicies.Query,
            [CacheTags.Reactions],
            cancellationToken);

        return Result.Success(page);
    }

    private IQueryable<Reaction> Candidates(ListReactionsQuery query)
    {
        var reactions = context.Reactions.AsNoTracking();

        return query.AvailableSubstanceIds is { Count: > 0 } available
            ? matching.PossibleWith(reactions, available, query.Match)
            : reactions;
    }

    private async Task<CursorPagedResult<ReactionSummaryResponse>> BrowseAsync(
        Cursor? cursor,
        ListReactionsQuery query,
        CancellationToken cancellationToken)
    {
        var reactions = Candidates(query);

        if (query.AvailableSubstanceIds is { Count: > 0 } available)
        {
            var ranked = await ReactionQueries.SummariesAsync(
                matching.NearestFirst(reactions, available).Take(query.Page.Limit),
                context.Substances.AsNoTracking(),
                query.Locale,
                cancellationToken);

            return new CursorPagedResult<ReactionSummaryResponse>
            {
                Items = ranked,
                PageSize = query.Page.Limit,
                HasMore = false,
                NextCursor = null
            };
        }

        if (cursor is not null)
        {
            var after = ReactionId.From(cursor.AfterId);
            reactions = reactions.Where(reaction => reaction.Id > after);
        }

        var rows = await ReactionQueries.SummariesAsync(
            reactions.OrderBy(reaction => reaction.Id).Take(query.Page.Limit + 1),
            context.Substances.AsNoTracking(),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, last => Cursor.After(last.Id));
    }

    public async Task<CursorPagedResult<ReactionSummaryResponse>> SearchAsync(
        Cursor? cursor,
        ListReactionsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = cursor?.Skip ?? 0;

        var rows = await ReactionQueries.SummariesAsync(
            search.Matching(Candidates(query), query.Search!.Trim())
                .ThenBy(reaction => reaction.Id)
                .Skip(skip)
                .Take(query.Page.Limit + 1),
            context.Substances.AsNoTracking(),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, _ => Cursor.Skipping(skip + query.Page.Limit));
    }

    private static CursorPagedResult<ReactionSummaryResponse> Page(
        IReadOnlyList<ReactionSummaryResponse> rows,
        int pageSize,
        Func<ReactionSummaryResponse, Cursor> next)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.Take(pageSize).ToList() : rows;

        return new CursorPagedResult<ReactionSummaryResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? next(items[^1]).Encode() : null
        };
    }
}
