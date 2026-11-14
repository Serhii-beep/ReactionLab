namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record RegisteredProfile(string DisplayName, string PreferredLocale, int BirthYear, string? TimeZone);
