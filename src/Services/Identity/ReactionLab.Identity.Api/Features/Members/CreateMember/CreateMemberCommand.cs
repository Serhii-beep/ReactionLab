namespace ReactionLab.Identity.Api.Features.Members.CreateMember;

internal sealed record CreateMemberCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    string? DisplayName,
    string? Username,
    string? Email,
    string? Locale,
    string? Role,
    Guid? SchoolId,
    string? Credential);
