namespace ReactionLab.Identity.Api.Features.Members.ResetMemberPassword;

internal sealed record ResetMemberPasswordCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    Guid AccountId,
    string? Credential);
