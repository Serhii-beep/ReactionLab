namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed record AdminSetupState(
    string? Username,
    long Version,
    bool HasPassword,
    bool HasEmail,
    bool IsDisabled,
    bool OrganizationHasAdminWhoCanSignIn);
