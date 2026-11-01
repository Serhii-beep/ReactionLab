namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record RenamedMember(string DisplayName, string PreferredLocale, long Version, bool DisplayNameChanged);
