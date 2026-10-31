namespace ReactionLab.Identity.Api.Features.Members.RenameMember;

internal sealed record RenameMemberCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    Guid AccountId,
    string? DisplayName,
    string? Username);
