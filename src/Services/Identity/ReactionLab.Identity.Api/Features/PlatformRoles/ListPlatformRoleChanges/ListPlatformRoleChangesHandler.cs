using ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleChanges;

internal sealed class ListPlatformRoleChangesHandler(AppDbContext context)
    : IQueryHandler<ListPlatformRoleChangesQuery, CursorPagedResult<PlatformRoleChangeResponse>>
{
    private const string GrantedChange = "granted";
    private const string RevokedChange = "revoked";

    public async ValueTask<Result<CursorPagedResult<PlatformRoleChangeResponse>>> HandleAsync(
        ListPlatformRoleChangesQuery query,
        CancellationToken cancellationToken)
    {
        var before = Guid.AllBitsSet;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<PlatformRoleChangeResponse>>(Cursor.Malformed);
            }

            before = cursor.Value.AfterId;
        }

        var changes = await context.ListPlatformRoleChangesAsync(
            query.AccountId,
            before,
            query.Page.Limit + 1,
            cancellationToken);

        var hasMore = changes.Count > query.Page.Limit;
        var items = changes
            .Take(query.Page.Limit)
            .Select(change => new PlatformRoleChangeResponse(
                change.Id,
                change.AccountId,
                change.AccountDisplayName,
                change.Role,
                change.Granted ? GrantedChange : RevokedChange,
                change.Source,
                change.ChangedBy,
                change.ChangedByDisplayName,
                change.ChangedAt))
            .ToList();

        return Result.Success(new CursorPagedResult<PlatformRoleChangeResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].Id).Encode() : null
        });
    }
}
