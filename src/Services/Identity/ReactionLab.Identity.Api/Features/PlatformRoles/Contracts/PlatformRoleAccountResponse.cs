namespace ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;

internal sealed record PlatformRoleAccountResponse(
    Guid AccountId,
    string DisplayName,
    string? Email,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles);
