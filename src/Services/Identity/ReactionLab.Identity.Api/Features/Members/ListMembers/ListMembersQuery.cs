using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Members.ListMembers;

internal sealed record ListMembersQuery(
    Guid ActorId,
    Guid OrganizationId,
    string? Search,
    string? Role,
    string? Status,
    CursorRequest Page);
