using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record MemberResponse(
    Guid AccountId,
    Guid OrganizationId,
    string Username,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    string Status,
    bool HasPendingSetup,
    DateTimeOffset CreatedAt)
{
    public static MemberResponse From(SchoolMember member, DateTimeOffset now) =>
        new(
            member.AccountId,
            member.OrganizationId,
            member.Username,
            member.DisplayName,
            member.Email,
            member.Roles,
            MemberStatuses.Of(member, now),
            member.HasPendingSetup,
            member.CreatedAt);
}
