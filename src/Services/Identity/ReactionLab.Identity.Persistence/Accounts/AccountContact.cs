namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record AccountContact(string? Email, bool EmailConfirmed, string DisplayName, string PreferredLocale);
