using ReactionLab.Identity.Api.Features.Members;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Organizations.ListOrganizationChanges;

internal sealed class ListOrganizationChangesHandler(SchoolAdminTransaction adminTransaction, AppDbContext context)
    : IQueryHandler<ListOrganizationChangesQuery, CursorPagedResult<OrganizationChangeResponse>>
{
    public async ValueTask<Result<CursorPagedResult<OrganizationChangeResponse>>> HandleAsync(
        ListOrganizationChangesQuery query,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.AuthorizeAsync(
            query.ActorId,
            query.OrganizationId,
            SchoolAdminRules.Admin,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<CursorPagedResult<OrganizationChangeResponse>>(access.Error);
        }

        Guid? before = null;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<OrganizationChangeResponse>>(Cursor.Malformed);
            }

            before = cursor.Value.AfterId;
        }

        var changes = await context.ListOrganizationChangesAsync(
            query.OrganizationId,
            before,
            query.Page.Limit + 1,
            cancellationToken);

        var hasMore = changes.Count > query.Page.Limit;
        var items = changes
            .Take(query.Page.Limit)
            .Select(OrganizationChangeResponse.From)
            .ToList();

        return Result.Success(new CursorPagedResult<OrganizationChangeResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].Id).Encode() : null
        });
    }
}
