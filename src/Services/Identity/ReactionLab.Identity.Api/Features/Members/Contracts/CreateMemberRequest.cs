namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record CreateMemberRequest(
    string? DisplayName,
    string? Username,
    string? Email,
    string? Locale,
    string? Role,
    Guid? SchoolId,
    string? Credential);
