namespace ReactionLab.Identity.Persistence.OpenId;

public sealed record ReauthenticationState(bool AccountHasPassword, DateTimeOffset? PasswordVerifiedAt);
