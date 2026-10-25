using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Organizations.ListOrganizations;

internal sealed class ListOrganizationsHandler(AppDbContext context)
    : IQueryHandler<ListOrganizationsQuery, CursorPagedResult<OrganizationResponse>>
{
    public async ValueTask<Result<CursorPagedResult<OrganizationResponse>>> HandleAsync(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var after = Guid.Empty;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<OrganizationResponse>>(Cursor.Malformed);
            }

            after = cursor.Value.AfterId;
        }

        var organizations = await context.ListOrganizationsAsync(
            query.DistrictId,
            after,
            query.Page.Limit + 1,
            cancellationToken);

        var hasMore = organizations.Count > query.Page.Limit;
        var items = organizations
            .Take(query.Page.Limit)
            .Select(OrganizationResponse.From)
            .ToList();

        return Result.Success(new CursorPagedResult<OrganizationResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].Id).Encode() : null
        });
    }
}
