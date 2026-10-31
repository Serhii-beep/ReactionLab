namespace ReactionLab.Identity.Api.Features.Members.ImportMembers;

internal sealed record ImportMembersCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    string? Csv,
    string? Credential);
