namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed record PlatformRoleAccount(
    Guid AccountId,
    string DisplayName,
    string? Email,
    bool EmailConfirmed,
    string[] Roles);
