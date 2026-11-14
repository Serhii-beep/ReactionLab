namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed record AccountSetupDescription(
    string Purpose,
    string OrganizationName,
    string SignInCode,
    string? Username,
    List<string> Roles,
    string? RequestedByDisplayName,
    bool RequestedByPlatformAdmin,
    List<string> RequestedByRoles);
