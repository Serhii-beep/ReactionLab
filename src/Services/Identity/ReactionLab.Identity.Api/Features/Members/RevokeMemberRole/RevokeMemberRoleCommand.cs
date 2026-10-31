namespace ReactionLab.Identity.Api.Features.Members.RevokeMemberRole;

internal sealed record RevokeMemberRoleCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    Guid AccountId,
    string Role);
