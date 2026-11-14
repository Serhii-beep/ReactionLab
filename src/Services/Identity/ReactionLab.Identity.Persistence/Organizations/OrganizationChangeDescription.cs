namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record OrganizationChangeDescription(
    DateTimeOffset ChangedAt,
    string OrganizationName,
    string OrganizationKind,
    string Change,
    string? Role,
    string? AccountDisplayName,
    string? AccountUsername,
    string? ChangedByDisplayName,
    bool ChangedByPlatform,
    int AccountCount);
