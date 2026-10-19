using ReactionLab.Identity.Api.Features.Sessions.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Sessions.ListSessions;

internal sealed class ListSessionsHandler(AppDbContext context, TimeProvider timeProvider)
    : IQueryHandler<ListSessionsQuery, CursorPagedResult<SessionResponse>>
{
    public async ValueTask<Result<CursorPagedResult<SessionResponse>>> HandleAsync(
        ListSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var before = Guid.AllBitsSet;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<SessionResponse>>(Cursor.Malformed);
            }

            before = cursor.Value.AfterId;
        }

        var sessions = await context.ListLiveSessionsAsync(
            query.AccountId.ToString(),
            before,
            query.Page.Limit + 1,
            timeProvider.GetUtcNow(),
            cancellationToken);

        var hasMore = sessions.Count > query.Page.Limit;
        var items = sessions
            .Take(query.Page.Limit)
            .Select(session => new SessionResponse(
                session.Id,
                session.Device,
                NetworkPrefixes.Of(session.Address),
                session.SignedInAt,
                session.LastActiveAt,
                session.ExpiresAt,
                session.Id == query.CurrentSessionId))
            .ToList();

        return Result.Success(new CursorPagedResult<SessionResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].Id).Encode() : null
        });
    }
}
