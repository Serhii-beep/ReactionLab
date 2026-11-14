namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed record PlatformRoleChangeDescription(
    DateTimeOffset ChangedAt,
    string Role,
    bool Granted,
    string Source,
    string? AccountDisplayName,
    string? AccountEmail,
    string? ChangedByDisplayName,
    string? ChangedByEmail,
    bool RecipientIsAdmin);
