namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record SchoolAccountAccess(Guid OrganizationId, IReadOnlyList<string> Roles);
