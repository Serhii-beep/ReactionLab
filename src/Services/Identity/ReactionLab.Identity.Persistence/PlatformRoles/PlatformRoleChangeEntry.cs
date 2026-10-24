namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed record PlatformRoleChangeEntry(
    Guid Id,
    Guid AccountId,
    string? AccountDisplayName,
    string Role,
    bool Granted,
    string Source,
    Guid? ChangedBy,
    string? ChangedByDisplayName,
    DateTimeOffset ChangedAt);
