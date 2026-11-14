namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record AccountLockout(
    bool LockoutEnabled,
    DateTimeOffset? SignInLockoutEnd,
    int SignInFailures,
    DateTimeOffset? PasswordCheckLockoutEnd,
    int PasswordCheckFailures);
