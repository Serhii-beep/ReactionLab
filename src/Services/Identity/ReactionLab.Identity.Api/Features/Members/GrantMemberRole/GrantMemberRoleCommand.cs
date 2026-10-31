namespace ReactionLab.Identity.Api.Features.Members.GrantMemberRole;

internal sealed record GrantMemberRoleCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    Guid AccountId,
    string Role);
