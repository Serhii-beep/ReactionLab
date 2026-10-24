namespace ReactionLab.Identity.Persistence.PlatformRoles;

public sealed record PlatformRoleCandidate(Guid AccountId, bool EmailConfirmed);
