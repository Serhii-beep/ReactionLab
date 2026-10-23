namespace ReactionLab.Identity.Persistence.OpenId;

public sealed record ReauthenticationState(
    bool AccountHasPassword,
    DateTimeOffset? PasswordProbationEndsAt,
    bool SessionValid,
    DateTimeOffset? PasswordVerifiedAt);
