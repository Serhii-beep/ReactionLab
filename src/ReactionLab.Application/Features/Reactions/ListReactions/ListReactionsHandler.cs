using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Application.Features.Reactions.ListReactions;

public sealed class ListReactionsHandler(
    IAppDbContext context,
    ICatalogSearch search,
    IReactionMatching matching)
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

        var reactions = context.Reactions.AsNoTracking();

        if (query.AvailableSubstanceIds is { Count: > 0 } available)
        {
            reactions = matching.PossibleWith(reactions, available);
        }

        return string.IsNullOrWhiteSpace(query.Search)
            ? await BrowseAsync(reactions, cursor, query, cancellationToken)
            : await SearchAsync(reactions, cursor, query, cancellationToken);
    }

    private async Task<Result<CursorPagedResult<ReactionSummaryResponse>>> BrowseAsync(
        IQueryable<Reaction> reactions,
        Cursor? cursor,
        ListReactionsQuery query,
        CancellationToken cancellationToken)
    {
        if (cursor is not null)
        {
            if (!cursor.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(Cursor.Malformed);
            }

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

    public async Task<Result<CursorPagedResult<ReactionSummaryResponse>>> SearchAsync(
        IQueryable<Reaction> reactions,
        Cursor? cursor,
        ListReactionsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = 0;

        if (cursor is not null)
        {
            if (cursor.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(Cursor.Malformed);
            }

            skip = cursor.Skip;
        }

        if (skip >= MaximumSearchDepth)
        {
            return Result.Failure<CursorPagedResult<ReactionSummaryResponse>>(
                Error.Validation(
                    "Reaction.SearchTooDeep",
                    "Search results are limited.",
                    field: "Cursor")
                    .WithArgs(("max", MaximumSearchDepth)));
        }

        var rows = await ReactionQueries.SummariesAsync(
            search.Matching(reactions, query.Search!.Trim())
                .ThenBy(reaction => reaction.Id)
                .Skip(skip)
                .Take(query.Page.Limit + 1),
            context.Substances.AsNoTracking(),
            query.Locale,
            cancellationToken);

        return Page(rows, query.Page.Limit, _ => Cursor.Skipping(skip + query.Page.Limit));
    }

    private static Result<CursorPagedResult<ReactionSummaryResponse>> Page(
        IReadOnlyList<ReactionSummaryResponse> rows,
        int pageSize,
        Func<ReactionSummaryResponse, Cursor> next)
    {
        var hasMore = rows.Count > pageSize;
        var items = hasMore ? rows.Take(pageSize).ToList() : rows;

        return Result.Success(new CursorPagedResult<ReactionSummaryResponse>
        {
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? next(items[^1]).Encode() : null
        });
    }
}
