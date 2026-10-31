using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Members.ListMembers;

internal sealed class ListMembersHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider) : IQueryHandler<ListMembersQuery, CursorPagedResult<MemberResponse>>
{
    public async ValueTask<Result<CursorPagedResult<MemberResponse>>> HandleAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.AuthorizeAsync(
            query.ActorId,
            query.OrganizationId,
            SchoolAdminRules.AdminOrTeacher,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<CursorPagedResult<MemberResponse>>(access.Error);
        }

        var search = ReadSearch(query, access.Value);

        if (search.IsFailure)
        {
            return Result.Failure<CursorPagedResult<MemberResponse>>(search.Error);
        }

        var after = Guid.Empty;

        if (!string.IsNullOrWhiteSpace(query.Page.Cursor))
        {
            var cursor = Cursor.Decode(query.Page.Cursor);

            if (cursor.IsFailure || !cursor.Value.IsKeyset)
            {
                return Result.Failure<CursorPagedResult<MemberResponse>>(Cursor.Malformed);
            }

            after = cursor.Value.AfterId;
        }

        var now = timeProvider.GetUtcNow();
        var members = await context.ListMembersAsync(
            query.OrganizationId,
            access.Value.DistrictId,
            search.Value,
            after,
            query.Page.Limit + 1,
            now,
            cancellationToken);

        var hasMore = members.Count > query.Page.Limit;
        var items = members
            .Take(query.Page.Limit)
            .Select(member => MemberResponse.From(member, now))
            .Select(member => access.Value.IsAdmin ? member : member with { Email = null })
            .ToList();

        return Result.Success(new CursorPagedResult<MemberResponse>
        {
            Items = items,
            PageSize = query.Page.Limit,
            HasMore = hasMore,
            NextCursor = hasMore ? Cursor.After(items[^1].AccountId).Encode() : null
        });
    }

    private static Result<MemberSearch> ReadSearch(ListMembersQuery query, OrganizationAccess access)
    {
        var text = ReadSearchText(query.Search);

        if (text.IsFailure)
        {
            return text.Error;
        }

        if (query.Role is { } role && !SchoolRoleNames.All.Contains(role))
        {
            return MemberErrors.RoleUnknown;
        }

        if (query.Status is { } status && !MemberStatuses.All.Contains(status))
        {
            return MemberErrors.StatusUnknown;
        }

        return new MemberSearch(text.Value, access.IsAdmin ? query.Role : SchoolRoleNames.Student, query.Status);
    }

    private static Result<string?> ReadSearchText(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Result.Success<string?>(null);
        }

        if (!UnicodeText.TryNormalize(search.Trim(), out var normalized) || normalized.Any(char.IsControl))
        {
            return MemberErrors.SearchInvalid;
        }

        return normalized.Length > MemberSearch.MaximumTextLength
            ? MemberErrors.SearchTooLong
            : normalized.ToLowerInvariant();
    }
}
