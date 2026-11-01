namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record UpdatedProfile(
    string? Email,
    string DisplayName,
    string PreferredLocale,
    bool HasPassword,
    long Version,
    bool ProfileChanged);
