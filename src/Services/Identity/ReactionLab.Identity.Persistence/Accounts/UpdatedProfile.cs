namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record UpdatedProfile(
    string? Email,
    string DisplayName,
    string PreferredLocale,
    string? TimeZone,
    bool HasPassword,
    long Version,
    bool ProfileChanged);
