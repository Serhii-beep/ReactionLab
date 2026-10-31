namespace ReactionLab.Identity.Persistence.Organizations;

public sealed record SchoolAccountAccess(Guid OrganizationId, bool IsSuspended, IReadOnlyList<string> Roles);
