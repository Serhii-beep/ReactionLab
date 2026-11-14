namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record AccountProfile(
    string? Email,
    string DisplayName,
    string PreferredLocale,
    string? TimeZone,
    bool HasPassword);
