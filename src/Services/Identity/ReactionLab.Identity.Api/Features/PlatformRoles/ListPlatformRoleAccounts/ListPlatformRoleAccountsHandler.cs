using ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleAccounts;

internal sealed class ListPlatformRoleAccountsHandler(AppDbContext context)
    : IQueryHandler<ListPlatformRoleAccountsQuery, CursorPagedResult<PlatformRoleAccountResponse>>
{
    public async ValueTask<Result<CursorPagedResult<PlatformRoleAccountResponse>>> HandleAsync(
        ListPlatformRoleAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var after = Guid.Empty;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<PlatformRoleAccountResponse>>(Cursor.Malformed);
            }

            after = cursor.Value.AfterId;
        }

        var accounts = await context.ListPlatformRoleAccountsAsync(after, query.Page.Limit + 1, cancellationToken);

        var hasMore = accounts.Count > query.Page.Limit;
        var items = accounts
            .Take(query.Page.Limit)
            .Select(account => new PlatformRoleAccountResponse(
                account.AccountId,
                account.DisplayName,
                account.Email,
                account.EmailConfirmed,
                account.Roles))
            .ToList();

        return Result.Success(new CursorPagedResult<PlatformRoleAccountResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].AccountId).Encode() : null
        });
    }
}
