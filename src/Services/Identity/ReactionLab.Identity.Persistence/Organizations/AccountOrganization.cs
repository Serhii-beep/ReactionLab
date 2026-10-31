namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record AccountOrganization(
    Guid OrganizationId,
    string Kind,
    string Name,
    bool IsSuspended,
    IReadOnlyList<string> Roles);
