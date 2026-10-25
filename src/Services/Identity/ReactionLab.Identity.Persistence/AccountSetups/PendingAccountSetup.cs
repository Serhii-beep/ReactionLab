namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed record PendingAccountSetup(
    Guid AccountId,
    string DisplayName,
    string Username,
    string OrganizationName,
    string SignInCode,
    DateTimeOffset ExpiresAt);
