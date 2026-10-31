namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record SchoolMember(
    Guid AccountId,
    Guid OrganizationId,
    string Username,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Roles,
    bool HasPassword,
    bool HasPendingSetup,
    DateTimeOffset? DisabledAt,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset CreatedAt);
